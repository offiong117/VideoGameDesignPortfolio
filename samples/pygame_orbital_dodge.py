"""
Orbital Dodge — a tiny PyGame arcade prototype.

A small hobbyist project that explores three things at once:
  - smooth polar-coordinate movement (the player orbits the center)
  - a deterministic-but-feeling-random asteroid spawner
  - a minimal entity loop without an ECS

Run:
    pip install pygame
    python pygame_orbital_dodge.py

Controls:
    LEFT / RIGHT   — orbit the planet
    UP   / DOWN    — push outward / pull inward
    R              — restart after a crash
"""

import math
import random
import sys
import pygame

# --- tunable constants -------------------------------------------------------
WIDTH, HEIGHT = 720, 720
CENTER = (WIDTH // 2, HEIGHT // 2)
FPS = 60

PLANET_RADIUS = 38
PLAYER_RADIUS = 9
ASTEROID_RADIUS_RANGE = (8, 22)

PLAYER_MIN_ORBIT = 70
PLAYER_MAX_ORBIT = 320
PLAYER_ANGULAR_SPEED = 2.6      # radians / sec at base orbit
PLAYER_RADIAL_SPEED = 110.0     # pixels / sec
ASTEROID_SPAWN_INTERVAL = 0.55  # seconds, decays over time


# --- helpers -----------------------------------------------------------------
def polar_to_xy(cx, cy, r, theta):
    return (cx + r * math.cos(theta), cy + r * math.sin(theta))


class Player:
    def __init__(self):
        self.theta = -math.pi / 2  # start at the top
        self.orbit = 180.0
        self.alive = True

    def update(self, dt, keys):
        # angular speed feels more natural if it scales inversely with radius
        ang = PLAYER_ANGULAR_SPEED * (180.0 / max(self.orbit, 1.0))
        if keys[pygame.K_LEFT]:
            self.theta -= ang * dt
        if keys[pygame.K_RIGHT]:
            self.theta += ang * dt
        if keys[pygame.K_UP]:
            self.orbit += PLAYER_RADIAL_SPEED * dt
        if keys[pygame.K_DOWN]:
            self.orbit -= PLAYER_RADIAL_SPEED * dt
        self.orbit = max(PLAYER_MIN_ORBIT, min(PLAYER_MAX_ORBIT, self.orbit))

    @property
    def position(self):
        return polar_to_xy(*CENTER, self.orbit, self.theta)

    def draw(self, surface):
        x, y = self.position
        pygame.draw.circle(surface, (240, 240, 255), (int(x), int(y)), PLAYER_RADIUS)
        # tiny trail back toward the planet
        tx, ty = polar_to_xy(*CENTER, self.orbit - 14, self.theta)
        pygame.draw.line(surface, (90, 110, 170), (int(tx), int(ty)), (int(x), int(y)), 2)


class Asteroid:
    __slots__ = ("x", "y", "vx", "vy", "r", "alive")

    def __init__(self):
        # spawn just outside the play field, aimed loosely at the planet
        edge_theta = random.uniform(0, math.tau)
        spawn_r = max(WIDTH, HEIGHT) * 0.55
        self.x, self.y = polar_to_xy(*CENTER, spawn_r, edge_theta)
        target = (
            CENTER[0] + random.uniform(-60, 60),
            CENTER[1] + random.uniform(-60, 60),
        )
        dx, dy = target[0] - self.x, target[1] - self.y
        norm = math.hypot(dx, dy) or 1.0
        speed = random.uniform(85, 160)
        self.vx = dx / norm * speed
        self.vy = dy / norm * speed
        self.r = random.uniform(*ASTEROID_RADIUS_RANGE)
        self.alive = True

    def update(self, dt):
        self.x += self.vx * dt
        self.y += self.vy * dt
        # cull when far off-screen
        if not (-80 <= self.x <= WIDTH + 80 and -80 <= self.y <= HEIGHT + 80):
            self.alive = False

    def draw(self, surface):
        pygame.draw.circle(surface, (200, 150, 110), (int(self.x), int(self.y)), int(self.r))


def collide(player_pos, asteroid):
    dx = player_pos[0] - asteroid.x
    dy = player_pos[1] - asteroid.y
    return dx * dx + dy * dy <= (PLAYER_RADIUS + asteroid.r) ** 2


def main():
    pygame.init()
    screen = pygame.display.set_mode((WIDTH, HEIGHT))
    pygame.display.set_caption("Orbital Dodge")
    clock = pygame.time.Clock()
    font = pygame.font.SysFont("menlo", 18)

    player = Player()
    asteroids = []
    spawn_timer = 0.0
    elapsed = 0.0

    while True:
        dt = clock.tick(FPS) / 1000.0
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                pygame.quit()
                sys.exit()
            if event.type == pygame.KEYDOWN and event.key == pygame.K_r and not player.alive:
                player = Player()
                asteroids.clear()
                spawn_timer = 0.0
                elapsed = 0.0

        keys = pygame.key.get_pressed()
        if player.alive:
            elapsed += dt
            player.update(dt, keys)
            spawn_timer += dt
            interval = max(0.15, ASTEROID_SPAWN_INTERVAL - elapsed * 0.01)
            while spawn_timer >= interval:
                spawn_timer -= interval
                asteroids.append(Asteroid())
            for a in asteroids:
                a.update(dt)
            asteroids = [a for a in asteroids if a.alive]
            for a in asteroids:
                if collide(player.position, a):
                    player.alive = False
                    break

        screen.fill((10, 12, 24))
        # planet
        pygame.draw.circle(screen, (90, 140, 220), CENTER, PLANET_RADIUS)
        pygame.draw.circle(screen, (40, 80, 140), CENTER, PLANET_RADIUS, 2)
        for a in asteroids:
            a.draw(screen)
        player.draw(screen)

        hud = font.render(f"time: {elapsed:5.1f}s   asteroids: {len(asteroids)}", True, (200, 200, 220))
        screen.blit(hud, (12, 10))
        if not player.alive:
            msg = font.render("crashed — press R to restart", True, (255, 180, 180))
            screen.blit(msg, (WIDTH // 2 - msg.get_width() // 2, HEIGHT // 2 + 80))

        pygame.display.flip()


if __name__ == "__main__":
    main()
