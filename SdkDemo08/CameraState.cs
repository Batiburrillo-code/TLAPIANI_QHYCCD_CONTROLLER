using System;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace SdkDemo08
{
    /// <summary>
    /// Clase para manejar el estado y parámetros de cada cámara individual
    /// </summary>
    public class CameraState
    {
        // Identificación
        public int CameraIndex { get; set; } // 0-3
        public string CameraName { get; set; } // "Cámara 1", "Cámara 2", etc.
        public StringBuilder CameraID { get; set; }
        public IntPtr CameraHandle { get; set; }
        public bool IsConnected { get; set; }
        public bool IsSelectedForMasterCapture { get; set; } // Para captura maestra

        // Parámetros de imagen
        public uint ReadMode { get; set; }
        public uint ReadModeWidth { get; set; }
        public uint ReadModeHeight { get; set; }
        public uint BinX { get; set; }
        public uint BinY { get; set; }
        public uint ImageBits { get; set; }
        public bool ColorOnOff { get; set; }
        public string ImageFileFormat { get; set; } // "FITS" or "PNG"

        // Capacidades
        public bool CanLive { get; set; }
        public bool CanSingle { get; set; }
        public bool CanSetColor { get; set; }
        public bool CanSet8Bits { get; set; }
        public bool CanSet16Bits { get; set; }
        public bool CanBIN1X1 { get; set; }
        public bool CanBIN2X2 { get; set; }
        public bool CanBIN3X3 { get; set; }
        public bool CanBIN4X4 { get; set; }
        public bool CanSetExpTime { get; set; }
        public bool CanSetGain { get; set; }
        public bool CanSetOffset { get; set; }
        public bool CanSetTraffic { get; set; }
        public bool CanSetGPS { get; set; }
        public bool CanSetCFW { get; set; }
        public bool CanCooler { get; set; }
        public bool CanHumidity { get; set; }
        public bool CanPressure { get; set; }
        public bool CanIgnoreOS { get; set; }

        // Parámetros de captura
        public double ExpTime { get; set; }
        public double Gain { get; set; }
        public double Offset { get; set; }
        public double Traffic { get; set; }
        public uint StreamMode { get; set; }

        // Información del chip
        public double ChipWidth { get; set; }
        public double ChipHeight { get; set; }
        public double PixelWidth { get; set; }
        public double PixelHeight { get; set; }
        public uint ImageWidth { get; set; }
        public uint ImageHeight { get; set; }
        public uint MaxImageWidth { get; set; }
        public uint MaxImageHeight { get; set; }
        public uint MaxResolution { get; set; }
        public uint MinImageWidth { get; set; }
        public uint MinImageHeight { get; set; }
        public uint MinResolution { get; set; }

        // ROI
        public uint ImageStartX { get; set; }
        public uint ImageStartY { get; set; }
        public uint ImageSizeX { get; set; }
        public uint ImageSizeY { get; set; }
        public uint EFStartX { get; set; }
        public uint EFStartY { get; set; }
        public uint EFSizeX { get; set; }
        public uint EFSizeY { get; set; }
        public uint OSStartX { get; set; }
        public uint OSStartY { get; set; }
        public uint OSSizeX { get; set; }
        public uint OSSizeY { get; set; }

        // Estado actual de imagen
        public uint CurImgWidth { get; set; }
        public uint CurImgHeight { get; set; }
        public uint CurImgBits { get; set; }
        public uint CurImgChannels { get; set; }

        // Modo de captura actual
        public string CaptureMode { get; set; } // "SINGLE", "LIVE", "RECORDING"
        public bool IsLiveActive { get; set; }
        public bool IsRecordingActive { get; set; }
        public bool IsRecordingPaused { get; set; }

        // Información de firmware
        public string FirmwareVersion { get; set; }
        public string FPGA1Version { get; set; }
        public string FPGA2Version { get; set; }
        public string SDKVersion { get; set; }

        // Rutas de guardado
        public string SaveFolderPath { get; set; }
        public string CurrentLiveSessionFolder { get; set; }
        public string RecordingSessionFolder { get; set; }
        public uint LiveFrameCounter { get; set; }
        public uint SingleFrameCounter { get; set; }
        public uint RecordingFrameCounter { get; set; }

        // Configuración de grabación
        public RecordingConfigForm RecordingConfig { get; set; }

        // Threads
        public Thread LiveImageThread { get; set; }
        public Thread SingleImageThread { get; set; }
        public Thread RecordingThread { get; set; }
        public Thread CoolerControlThread { get; set; }

        // Control de threads
        public bool Quit { get; set; }
        public bool HasQuit { get; set; }

        // Datos de imagen
        public byte[] RawArray { get; set; }
        public int ImageMemoryLength { get; set; }

        // Constructor
        public CameraState(int index)
        {
            CameraIndex = index;
            CameraName = string.Format("Cámara {0}", index + 1);
            CameraID = new StringBuilder(0);
            CameraHandle = IntPtr.Zero;
            IsConnected = false;
            IsSelectedForMasterCapture = false;
            
            // Valores por defecto
            ImageFileFormat = "FITS";
            ReadMode = 0;
            BinX = 1;
            BinY = 1;
            ImageBits = 16;
            ColorOnOff = false;
            StreamMode = 0;
            CaptureMode = "SINGLE";
            
            ExpTime = 100000; // 100ms por defecto
            Gain = 0;
            Offset = 0;
            Traffic = 0;
            
            Quit = false;
            HasQuit = false;
        }

        /// <summary>
        /// Obtiene el nombre de la carpeta para esta cámara
        /// </summary>
        public string GetCameraFolderName()
        {
            return string.Format("Cámara{0}", CameraIndex + 1);
        }
    }
}

