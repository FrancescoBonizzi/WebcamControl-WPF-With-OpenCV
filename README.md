# WebcamControl-WPF-With-OpenCV
A simple webcam control for WPF based on OpenCVSharp, now with a **QRCode reader from webcam stream**!

The goal of this "control" is to simplify the webcam streaming on WPF and be open to post processing like recognizing faces!

# ![Screenshot](images/screen.png)


## Notes
Cameras enumeration has been a pain in the ass.
With `OpenCv` you cannot connect to a camera by name, you have to use an index. The index in `OpenCv` is based on **the connection order to the computer**. Neither `WMI` gives you that information, so I had to reference `DirectShow(Lib)` which, luckly, does the same as `OpenCv`. (As far as I know!)