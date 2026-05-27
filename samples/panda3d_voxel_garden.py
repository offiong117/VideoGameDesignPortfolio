"""
Voxel Garden — a Panda3D learning project.

A tiny scene that procedurally grows a "garden" of colored voxel plants on a
grid. Built to practice three Panda3D concepts:
  - building geometry at runtime with GeomNode / GeomVertexWriter
  - task-based animation (Panda's `taskMgr`)
  - mouse-orbit camera math without an extension

Run:
    pip install panda3d
    python panda3d_voxel_garden.py

Controls:
    drag LMB   — orbit
    scroll     — zoom
    SPACE      — regrow the garden
"""

import math
import random

from direct.showbase.ShowBase import ShowBase
from panda3d.core import (
    GeomNode, Geom, GeomTriangles, GeomVertexData, GeomVertexFormat,
    GeomVertexWriter, NodePath, Vec3, Vec4, LineSegs, AmbientLight,
    DirectionalLight, WindowProperties,
)


def make_cube(color: Vec4) -> NodePath:
    """Build a unit cube as a NodePath. Practice for the Geom API."""
    fmt = GeomVertexFormat.getV3c4()
    vdata = GeomVertexData("cube", fmt, Geom.UHStatic)
    vdata.setNumRows(8)
    vw = GeomVertexWriter(vdata, "vertex")
    cw = GeomVertexWriter(vdata, "color")

    corners = [
        (-0.5, -0.5, -0.5), (0.5, -0.5, -0.5), (0.5, 0.5, -0.5), (-0.5, 0.5, -0.5),
        (-0.5, -0.5,  0.5), (0.5, -0.5,  0.5), (0.5, 0.5,  0.5), (-0.5, 0.5,  0.5),
    ]
    for c in corners:
        vw.addData3(*c)
        cw.addData4(color)

    faces = [
        (0, 1, 2, 3), (4, 6, 5, 4),  # bottom / top corrected below
        (4, 5, 6, 7),
        (0, 4, 7, 3),
        (1, 5, 6, 2),
        (0, 1, 5, 4),
        (3, 2, 6, 7),
    ]
    # rebuild faces cleanly to avoid the duplicate above
    faces = [
        (0, 1, 2, 3),  # -Z
        (4, 5, 6, 7),  # +Z
        (0, 4, 7, 3),  # -X
        (1, 5, 6, 2),  # +X
        (0, 1, 5, 4),  # -Y
        (3, 2, 6, 7),  # +Y
    ]
    tris = GeomTriangles(Geom.UHStatic)
    for a, b, c, d in faces:
        tris.addVertices(a, b, c)
        tris.addVertices(a, c, d)

    geom = Geom(vdata)
    geom.addPrimitive(tris)
    node = GeomNode("cube")
    node.addGeom(geom)
    return NodePath(node)


def make_ground_grid(size: int = 12) -> NodePath:
    seg = LineSegs()
    seg.setThickness(1.0)
    seg.setColor(0.35, 0.4, 0.45, 1.0)
    for i in range(-size, size + 1):
        seg.moveTo(i, -size, 0)
        seg.drawTo(i,  size, 0)
        seg.moveTo(-size, i, 0)
        seg.drawTo( size, i, 0)
    return NodePath(seg.create())


class VoxelGarden(ShowBase):
    def __init__(self):
        ShowBase.__init__(self)
        props = WindowProperties()
        props.setTitle("Voxel Garden")
        props.setSize(960, 720)
        self.win.requestProperties(props)
        self.setBackgroundColor(0.07, 0.08, 0.11, 1.0)

        # lighting
        ambient = AmbientLight("ambient")
        ambient.setColor(Vec4(0.35, 0.35, 0.4, 1.0))
        sun = DirectionalLight("sun")
        sun.setColor(Vec4(0.9, 0.85, 0.75, 1.0))
        amb_np = self.render.attachNewNode(ambient)
        sun_np = self.render.attachNewNode(sun)
        sun_np.setHpr(40, -55, 0)
        self.render.setLight(amb_np)
        self.render.setLight(sun_np)

        # ground
        make_ground_grid().reparentTo(self.render)

        # camera state
        self.cam_radius = 18.0
        self.cam_pitch = 28.0
        self.cam_yaw = 35.0
        self.dragging = False
        self.last_mouse = None
        self.disableMouse()
        self._update_camera()

        # input
        self.accept("mouse1", self._mouse_down)
        self.accept("mouse1-up", self._mouse_up)
        self.accept("wheel_up", lambda: self._zoom(-1.2))
        self.accept("wheel_down", lambda: self._zoom(1.2))
        self.accept("space", self.grow)

        self.plants_root = self.render.attachNewNode("plants")
        self.grow()
        self.taskMgr.add(self._mouse_task, "mouse-task")

    # ---- camera ----
    def _zoom(self, dr: float):
        self.cam_radius = max(6.0, min(40.0, self.cam_radius + dr))
        self._update_camera()

    def _mouse_down(self):
        self.dragging = True
        self.last_mouse = None

    def _mouse_up(self):
        self.dragging = False
        self.last_mouse = None

    def _mouse_task(self, task):
        if self.dragging and self.mouseWatcherNode.hasMouse():
            m = self.mouseWatcherNode.getMouse()
            mx, my = m.getX(), m.getY()
            if self.last_mouse is not None:
                dx = mx - self.last_mouse[0]
                dy = my - self.last_mouse[1]
                self.cam_yaw -= dx * 120.0
                self.cam_pitch = max(8.0, min(80.0, self.cam_pitch + dy * 90.0))
                self._update_camera()
            self.last_mouse = (mx, my)
        return task.cont

    def _update_camera(self):
        yaw = math.radians(self.cam_yaw)
        pitch = math.radians(self.cam_pitch)
        x = self.cam_radius * math.cos(pitch) * math.sin(yaw)
        y = -self.cam_radius * math.cos(pitch) * math.cos(yaw)
        z = self.cam_radius * math.sin(pitch)
        self.camera.setPos(x, y, z)
        self.camera.lookAt(0, 0, 2)

    # ---- garden ----
    def grow(self):
        self.plants_root.removeNode()
        self.plants_root = self.render.attachNewNode("plants")
        rng = random.Random()
        for gx in range(-5, 6):
            for gy in range(-5, 6):
                if rng.random() > 0.45:
                    continue
                height = rng.randint(1, 5)
                hue = rng.random()
                for h in range(height):
                    t = h / max(height - 1, 1)
                    color = Vec4(
                        0.25 + 0.4 * hue,
                        0.55 + 0.35 * (1 - t),
                        0.2 + 0.3 * t,
                        1.0,
                    )
                    cube = make_cube(color)
                    cube.setScale(0.9)
                    cube.setPos(gx, gy, 0.5 + h)
                    cube.reparentTo(self.plants_root)


if __name__ == "__main__":
    VoxelGarden().run()
