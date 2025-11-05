using System;
using System.IO;
using System.Text;

namespace SdkDemo08
{
    /// <summary>
    /// Clase para escribir archivos FITS (Flexible Image Transport System)
    /// Compatible con .NET Framework 3.5
    /// </summary>
    public class FITSWriter
    {
        private const int FITS_BLOCK_SIZE = 2880; // Tamaño estándar de bloque FITS (36 registros de 80 bytes)
        private const int HEADER_RECORD_SIZE = 80; // Tamaño de cada registro de header

        /// <summary>
        /// Escribe un archivo FITS con los datos RAW de la imagen
        /// </summary>
        /// <param name="filePath">Ruta completa del archivo a crear</param>
        /// <param name="rawData">Datos RAW de la imagen (8-bit o 16-bit)</param>
        /// <param name="width">Ancho de la imagen en píxeles</param>
        /// <param name="height">Alto de la imagen en píxeles</param>
        /// <param name="bitsPerPixel">Bits por píxel (8 o 16)</param>
        /// <param name="channels">Número de canales (1 para monocromo, 3 para color)</param>
        /// <param name="metadata">Metadata adicional para incluir en el header</param>
        public static void WriteFITS(string filePath, byte[] rawData, uint width, uint height, 
            uint bitsPerPixel, uint channels, FITSMetadata metadata)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    // Escribir header FITS
                    WriteHeader(writer, width, height, bitsPerPixel, channels, metadata);
                    
                    // Escribir datos de la imagen
                    WriteImageData(writer, rawData, width, height, bitsPerPixel, channels);
                }
            }
        }

        /// <summary>
        /// Escribe el header FITS con todos los metadatos
        /// </summary>
        private static void WriteHeader(BinaryWriter writer, uint width, uint height, 
            uint bitsPerPixel, uint channels, FITSMetadata metadata)
        {
            StringBuilder header = new StringBuilder();

            // Header básico requerido
            WriteHeaderKeyword(header, "SIMPLE", "T", "FITS file");
            WriteHeaderKeyword(header, "BITPIX", bitsPerPixel == 8 ? "8" : "16", "Bits per pixel");
            
            // NAXIS - número de ejes
            if (channels == 1)
            {
                WriteHeaderKeyword(header, "NAXIS", "2", "Number of axes");
                WriteHeaderKeyword(header, "NAXIS1", width.ToString(), "Width in pixels");
                WriteHeaderKeyword(header, "NAXIS2", height.ToString(), "Height in pixels");
            }
            else
            {
                WriteHeaderKeyword(header, "NAXIS", "3", "Number of axes");
                WriteHeaderKeyword(header, "NAXIS1", width.ToString(), "Width in pixels");
                WriteHeaderKeyword(header, "NAXIS2", height.ToString(), "Height in pixels");
                WriteHeaderKeyword(header, "NAXIS3", channels.ToString(), "Color channels");
            }

            if (bitsPerPixel == 16)
            {
                WriteHeaderKeyword(header, "BZERO", "32768", "Data offset");
                WriteHeaderKeyword(header, "BSCALE", "1", "Data scaling");
            }
            else
            {
                WriteHeaderKeyword(header, "BZERO", "0", "Data offset");
                WriteHeaderKeyword(header, "BSCALE", "1", "Data scaling");
            }
            WriteHeaderKeyword(header, "EXTEND", "T", "FITS file may contain extensions");

            // Metadata de cámara
            if (!string.IsNullOrEmpty(metadata.CameraModel))
                WriteHeaderKeyword(header, "INSTRUME", metadata.CameraModel, "Camera model");
            
            if (!string.IsNullOrEmpty(metadata.CameraID))
                WriteHeaderKeyword(header, "CAMERAID", metadata.CameraID, "Camera ID");

            // Metadata de captura
            WriteHeaderKeyword(header, "DATE-OBS", metadata.ObservationDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), "Date of observation (UTC)");
            WriteHeaderKeyword(header, "DATE", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff"), "File creation date (UTC)");
            WriteHeaderKeyword(header, "EXPTIME", metadata.ExposureTime.ToString("F6"), "Exposure time in seconds");
            WriteHeaderKeyword(header, "GAIN", metadata.Gain.ToString("F2"), "Camera gain");
            WriteHeaderKeyword(header, "OFFSET", metadata.Offset.ToString("F2"), "Camera offset");
            
            if (metadata.USBTraffic > 0)
                WriteHeaderKeyword(header, "USBTRAFF", metadata.USBTraffic.ToString("F2"), "USB traffic setting");

            // Metadata de imagen
            WriteHeaderKeyword(header, "IMGWIDTH", width.ToString(), "Image width in pixels");
            WriteHeaderKeyword(header, "IMGHEIGHT", height.ToString(), "Image height in pixels");
            WriteHeaderKeyword(header, "BITDEPTH", bitsPerPixel.ToString(), "Bit depth");
            WriteHeaderKeyword(header, "CHANNELS", channels.ToString(), "Number of color channels");
            
            WriteHeaderKeyword(header, "IMGTYPE", metadata.ImageType, "Image type (LIVE or SINGLE)");
            
            if (!string.IsNullOrEmpty(metadata.ReadMode))
                WriteHeaderKeyword(header, "READMODE", metadata.ReadMode, "Read mode");
            
            if (metadata.BinX > 0 && metadata.BinY > 0)
            {
                WriteHeaderKeyword(header, "XBINNING", metadata.BinX.ToString(), "Binning in X axis");
                WriteHeaderKeyword(header, "YBINNING", metadata.BinY.ToString(), "Binning in Y axis");
            }

            // Metadata de chip (si está disponible)
            if (metadata.ChipWidth > 0)
                WriteHeaderKeyword(header, "CHIPW", metadata.ChipWidth.ToString("F4"), "Chip width in mm");
            if (metadata.ChipHeight > 0)
                WriteHeaderKeyword(header, "CHIPH", metadata.ChipHeight.ToString("F4"), "Chip height in mm");
            if (metadata.PixelWidth > 0)
                WriteHeaderKeyword(header, "XPIXSZ", metadata.PixelWidth.ToString("F4"), "Pixel width in microns");
            if (metadata.PixelHeight > 0)
                WriteHeaderKeyword(header, "YPIXSZ", metadata.PixelHeight.ToString("F4"), "Pixel height in microns");

            // Metadata de temperatura (si está disponible)
            if (metadata.Temperature != double.MinValue)
                WriteHeaderKeyword(header, "CCD-TEMP", metadata.Temperature.ToString("F2"), "CCD temperature in Celsius");

            // Metadata de GPS (si está disponible)
            if (metadata.Latitude != double.MinValue)
                WriteHeaderKeyword(header, "LATITUDE", metadata.Latitude.ToString("F8"), "Latitude in degrees");
            if (metadata.Longitude != double.MinValue)
                WriteHeaderKeyword(header, "LONGITUD", metadata.Longitude.ToString("F8"), "Longitude in degrees");

            // Metadata de secuencia (si está disponible)
            if (metadata.SequenceNumber > 0)
                WriteHeaderKeyword(header, "SEQUENCE", metadata.SequenceNumber.ToString(), "Frame sequence number");
            
            if (metadata.FrameNumber > 0)
                WriteHeaderKeyword(header, "FRAMENUM", metadata.FrameNumber.ToString(), "Frame number in session");

            // Metadata adicional
            WriteHeaderKeyword(header, "SOFTWARE", "QHYCCD SDK Demo", "Software that created this file");
            WriteHeaderKeyword(header, "AUTHOR", "QHYCCD", "Author/Organization");

            // End marker
            WriteHeaderKeyword(header, "END", "", "");

            // Rellenar hasta completar bloques de 2880 bytes
            int headerLength = header.Length;
            int blocksNeeded = (int)Math.Ceiling((double)headerLength / FITS_BLOCK_SIZE);
            int totalHeaderSize = blocksNeeded * FITS_BLOCK_SIZE;
            
            // Escribir header con padding
            byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            writer.Write(headerBytes);
            
            // Rellenar con espacios
            int padding = totalHeaderSize - headerLength;
            byte[] paddingBytes = new byte[padding];
            for (int i = 0; i < padding; i++)
                paddingBytes[i] = 0x20; // Espacio ASCII
            
            writer.Write(paddingBytes);
        }

        /// <summary>
        /// Escribe una keyword del header FITS en formato estándar
        /// </summary>
        private static void WriteHeaderKeyword(StringBuilder header, string keyword, string value, string comment)
        {
            string line = keyword.PadRight(8);
            
            if (string.IsNullOrEmpty(value))
            {
                line += " " + "".PadRight(20);
            }
            else if (value == "T" || value == "F")
            {
                // Boolean
                line += " " + value.PadRight(20);
            }
            else if (IsNumeric(value))
            {
                // Número
                line += " " + value.PadLeft(20);
            }
            else
            {
                // String
                line += "= '" + value.PadRight(18) + "'";
            }
            
            if (!string.IsNullOrEmpty(comment))
            {
                line += " / " + comment;
            }
            
            // Completar a 80 caracteres
            line = line.PadRight(80);
            header.Append(line);
        }

        /// <summary>
        /// Verifica si un string es numérico
        /// </summary>
        private static bool IsNumeric(string value)
        {
            double result;
            return double.TryParse(value, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Escribe los datos de la imagen en formato FITS
        /// </summary>
        private static void WriteImageData(BinaryWriter writer, byte[] rawData, 
            uint width, uint height, uint bitsPerPixel, uint channels)
        {
            uint totalPixels = width * height * channels;
            uint dataSize;

            if (bitsPerPixel == 8)
            {
                dataSize = totalPixels;
                
                // Convertir bytes a formato FITS (little-endian para 8-bit está bien)
                // Para 16-bit, necesitamos convertir de little-endian a big-endian
                writer.Write(rawData, 0, (int)dataSize);
            }
            else // 16-bit
            {
                dataSize = totalPixels * 2;
                
                // Convertir de little-endian (Windows) a big-endian (FITS)
                for (int i = 0; i < totalPixels; i++)
                {
                    int byteIndex = i * 2;
                    if (byteIndex + 1 < rawData.Length)
                    {
                        // FITS requiere big-endian para 16-bit
                        byte low = rawData[byteIndex];
                        byte high = rawData[byteIndex + 1];
                        writer.Write(high);
                        writer.Write(low);
                    }
                    else
                    {
                        // Padding si es necesario
                        writer.Write((byte)0);
                        writer.Write((byte)0);
                    }
                }
            }

            // Rellenar hasta completar bloques de 2880 bytes
            int writtenBytes = (int)dataSize;
            int blocksNeeded = (int)Math.Ceiling((double)writtenBytes / FITS_BLOCK_SIZE);
            int totalDataSize = blocksNeeded * FITS_BLOCK_SIZE;
            int padding = totalDataSize - writtenBytes;
            
            if (padding > 0)
            {
                byte[] paddingBytes = new byte[padding];
                writer.Write(paddingBytes);
            }
        }
    }

    /// <summary>
    /// Clase para almacenar metadata de la imagen FITS
    /// </summary>
    public class FITSMetadata
    {
        public string CameraModel { get; set; }
        public string CameraID { get; set; }
        public DateTime ObservationDate { get; set; }
        public double ExposureTime { get; set; }
        public double Gain { get; set; }
        public double Offset { get; set; }
        public double USBTraffic { get; set; }
        public string ImageType { get; set; }
        public string ReadMode { get; set; }
        public uint BinX { get; set; }
        public uint BinY { get; set; }
        public double ChipWidth { get; set; }
        public double ChipHeight { get; set; }
        public double PixelWidth { get; set; }
        public double PixelHeight { get; set; }
        public double Temperature { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public uint SequenceNumber { get; set; }
        public uint FrameNumber { get; set; }

        public FITSMetadata()
        {
            Temperature = double.MinValue;
            Latitude = double.MinValue;
            Longitude = double.MinValue;
            ObservationDate = DateTime.UtcNow;
        }
    }
}

