# Cardboard Panorama Viewer

A simple 3D panorama viewer for Meta Quest, created by a beginner in programming with the help of artificial intelligence.

The goal is to display panoramic images taken with Google Cardboard Camera or Google Camera in Panorama Mode in a stereoscopic and immersive way using VR.

---

## Features

- Displays 3D panoramic images in VR
- Extracts and plays audio from image metadata if present
- Lists available panoramas in a simple menu
- Stereoscopic effect can be toggled on or off
- Extracted images are saved to persistent data for faster loading

---

## Where to put the panorama images

The app automatically creates the following folder on the device's external storage the first time it runs:
/storage/emulated/0/Pano/

Place your `.jpg` panoramic images in this folder.  

---

## Controls

- Press **Menu** button (three dots on left controller) to open or close the file selection menu
- Press **A** button (on right controller) to toggle stereoscopic effect

---

## Known limitations

- The image edges are not blurred beyond the visible area, which would improve the immersive experience
- The projection sphere is very basic; a better sphere mesh or a stereoscopic skybox shader would greatly improve the visual quality
- Extracting image and audio from metadata is slow the first time, but the result is cached in persistent data for faster loading later

---

## Created by

A curious beginner with the continuous support of ChatGPT and artificial intelligence.  
This is a learning project, built with passion and experimentation.

---

## Contributing

If you would like to improve the sphere mesh, add edge blur, optimize metadata extraction, or suggest new features, feel free to fork or submit a pull request.


