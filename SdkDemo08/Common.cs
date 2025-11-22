using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SdkDemo08
{
    class Common
    {
        public static IntPtr camHandle;

        public static StringBuilder camID = new StringBuilder(0);

        public static bool canLive;
        public static bool canSingle;

        public static int  length; //Image Memory Length
        public static uint camStreamMode; //
        public static uint camReadMode;
        public static uint camReadModeWidth, camReadModeHeight;

        public static bool canSetColor;
        public static bool camColorOnOff;

        public static bool canSet8Bits;
        public static bool canSet16Bits;
        public static uint camImageBits;

        public static bool canBIN1X1;
        public static bool canBIN2X2;
        public static bool canBIN3X3;
        public static bool canBIN4X4;
        public static bool canBIN6X6;
        public static bool canBIN8X8;
        public static uint camBinX, camBinY;

        public static double camChipWidth, camChipHeight, camPixelWidth, camPixelHeight; //Chip Info
        public static uint camImageWidth, camImageHeight; //Chip Info

        public static uint camEFStartX, camEFStartY, camEFSizeX, camEFSizeY; //Effective Area
        public static uint camOSStartX, camOSStartY, camOSSizeX, camOSSizeY; //Overscan Area

        public static uint camMaxImageWidth, camMaxImageHeight, camMaxResolution; //Max Size
        public static uint camMinImageWidth, camMinImageHeight, camMinResolution; //Min Size

        public static uint camImageStartX, camImageStartY, camImageSizeX, camImageSizeY; //ROI

        public static uint camCurImgWidth, camCurImgHeight, camCurImgBits, camCurImgChannels; //Get Single/Live Frame Data
        
        public static bool canIgoreOS;

        public static bool canSetExpTime;
        public static double camExpTime;

        public static bool canSetGain;
        public static double camGain;

        public static bool canSetOffset;
        public static double camOffset;

        public static bool canSetTraffic;
        public static double camTraffic;

        public static bool canSetSpeed;

        public static bool canSetGPS;

        public static bool canSetCFW;

        public static bool canCooler;

        public static bool canHumidity;

        public static bool canPressure;

        public static bool burstOnOff;
        public static bool burstCap;
        public static uint burstCapNum;
        public static uint burstCapTarget;

        public static string imageFileFormat = "FITS"; // "FITS" or "PNG"
    }
}
