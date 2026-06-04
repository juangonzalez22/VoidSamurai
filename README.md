<p align="center">
  <img src="Assets/logo.png" alt="Void Samurai Logo" width="728"/>
</p>

<h1 align="center">Void Samurai</h1>

<p align="center">
  A 2D fighting game built in Unity where martial precision meets computer vision.
</p>

---

## Overview

**Void Samurai** is a 2D fighting game inspired by classic titles such as *Street Fighter* and *Mortal Kombat*.  
Set inside a minimalist **dojo arena**, two warriors face each other:

- Player 1: Controlled using **computer vision (pose + object detection)**
- Player 2: Controlled using a **game controller**

The game blends traditional gameplay with real-time AI-based motion tracking.

---

## Core Technologies

- **Unity 6000.0.75f1**
- Unity 2D (Sprites, Animator, Physics, Scripts)
- Python (Computer Vision pipeline)
- YOLOv8 (Object Detection)
- Roboflow (Dataset creation & training)
- MediaPipe Pose (.task model)
- UDP Socket Communication (Python ↔ Unity)

---

## Gameplay Concept

Inside a sacred dojo, a samurai faces a rival ronin in a duel of skill and timing.

- Movement is driven by **real-time pose detection**
- Attacks are triggered by **object-based detection (weapon simulation)**
- Player 2 uses a traditional controller for competitive gameplay
