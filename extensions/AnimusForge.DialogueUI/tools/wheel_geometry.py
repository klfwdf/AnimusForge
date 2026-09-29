"""Sector geometry of afdui_wheel_chassis_symmetric.png (1024x1024), measured from the art.

Spokes at 0/90/180/240/300 degrees (math angles, counter-clockwise from +x). The clickable band is
the parchment ring between the hub (r=170 art px) and the gold rim (r=440 art px).
The C# hit test (src/Scene/WheelSectors.cs) must use the same numbers; verify_presentation.py checks it.
"""
ART_SIZE = 1024
INNER = 170
OUTER = 440
# name: (start_deg, end_deg), counter-clockwise.
SECTORS = {
    'actions': (0, 90),
    'talk': (90, 180),
    'rumor': (180, 240),
    'leave': (240, 300),
    'give': (300, 360),
}
