using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using System.Globalization;
using System.Collections;
using StructModel;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;



namespace SdkDemo08
{
    public partial class Form1 : Form
    {
        int b1 = 1, b2 = 1, b3 = 1, b4 = 1, b5 = 1, b6 = 1, b7 = 1, b8 = 1;

        int singleCount = 0, liveCount = 0;

        bool controlCooler = false;
        bool isSetupUI = false;

        Bitmap bitmap, bitmap2;
        Rectangle rectangle, rectangle2;
        BitmapData bmpData, bmpData2;

        IntPtr ptr, ptr2;

        int s, s2;
        int index, index2;

        double prog = 0.0;

        Byte pixData, pixData2;

        bool isConnect = false;
        bool isChangingMode = false; // Bandera para evitar recursión en cambio de modo
        System.Collections.Queue QHYQueue = new System.Collections.Queue();

        byte[] rawArray, rgbArray, rawArray2, rgbArray2;

        int retVal = -1, retVal2 = -1, camScanNum = 0, camScanNum2 = 0;

        StringBuilder id  = new StringBuilder(0);
        StringBuilder id2 = new StringBuilder(0);

        //int canCooler, canHumidity, canPressure;
        bool quit = false, hasquit = false;
        bool quitCoolerGet = false, hasQuitCoolerGet = false;
        bool quitGetStatus = false, hasQuitGetStatus = false;

        bool isInitCFW = true;
        double curCFWPos;
        string[] cfwNames = new string[10] { "Filter1", "Filter2", "Filter3", "Filter4", "Filter5", "Filter6", "Filter7", "Filter8", "Filter9", "Filter10"};

        Thread threadShowSingleImage;
        Thread threadShowLiveImage;
        Thread threadRecording;

        Thread threadControlCooler;

        Thread threadCameraStatus;

        // Variables para guardado de imágenes FITS
        string saveFolderPath = "";
        string currentLiveSessionFolder = "";
        uint liveFrameCounter = 0;
        uint singleFrameCounter = 0;
        DateTime liveSessionStartTime;

        // Variables para modo Recording
        bool isRecording = false;
        bool isRecordingPaused = false;
        RecordingConfigForm recordingConfig = null;
        string recordingSessionFolder = "";
        uint recordingFrameCounter = 0;
        uint recordingSequenceCounter = 0;
        DateTime recordingStartTime;
        ProgressBar progressBarRecording;
        Label labelRecordingProgress;
        Button btnPauseResume;

        public Form1()
        {
            try
            {
                InitializeComponent();
                // Agregar event handlers para cambio de modo
                this.radioBtnSingle.CheckedChanged += new EventHandler(radioBtnSingle_CheckedChanged);
                this.radioBtnLive.CheckedChanged += new EventHandler(radioBtnLive_CheckedChanged);
                this.radioBtnRecording.CheckedChanged += new EventHandler(radioBtnRecording_CheckedChanged);
                
                // Inicializar controles de grabación (barra de progreso y label)
                InitializeRecordingControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al inicializar el formulario:\n\n" + ex.Message + "\n\n" + ex.StackTrace, 
                    "Error de Inicialización", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-lanzar para que el Application.Run pueda manejarlo
            }
        }

        private void InitializeRecordingControls()
        {
            // Barra de progreso en la parte inferior derecha
            progressBarRecording = new ProgressBar();
            UpdateRecordingControlsPosition();
            progressBarRecording.Size = new System.Drawing.Size(200, 20);
            progressBarRecording.Visible = false;
            progressBarRecording.Minimum = 0;
            progressBarRecording.Maximum = 100;
            this.Controls.Add(progressBarRecording);
            progressBarRecording.BringToFront();

            // Label para mostrar progreso
            labelRecordingProgress = new Label();
            labelRecordingProgress.Size = new System.Drawing.Size(200, 20);
            labelRecordingProgress.Visible = false;
            labelRecordingProgress.Text = "";
            this.Controls.Add(labelRecordingProgress);
            labelRecordingProgress.BringToFront();

            // Botón Pausa/Reanudar
            btnPauseResume = new Button();
            btnPauseResume.Size = new System.Drawing.Size(100, 25);
            btnPauseResume.Text = "Pausa";
            btnPauseResume.Visible = false;
            btnPauseResume.Click += BtnPauseResume_Click;
            this.Controls.Add(btnPauseResume);
            btnPauseResume.BringToFront();

            // Agregar event handler para resize
            this.Resize += Form1_Resize;
        }

        private void UpdateRecordingControlsPosition()
        {
            if (progressBarRecording != null)
            {
                progressBarRecording.Location = new System.Drawing.Point(this.Width - 250, this.Height - 50);
            }
            if (labelRecordingProgress != null)
            {
                labelRecordingProgress.Location = new System.Drawing.Point(this.Width - 250, this.Height - 70);
            }
            if (btnPauseResume != null)
            {
                btnPauseResume.Location = new System.Drawing.Point(this.Width - 250, this.Height - 30);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            UpdateRecordingControlsPosition();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "C# Demo for GPS";
            this.tabPageGPS.Parent = null;
        }

        private void UpdateLocation(int i, int show)
        {
            Button[] btns = new Button[7];
            btns[0] = btnHeadConnect;
            btns[1] = btnHeadCapture;
            btns[2] = btnHeadParam;
            btns[3] = btnHeadGPS;
            btns[4] = btnHeadCFW;
            btns[5] = btnHeadCooler;
            btns[6] = btnHeadBurst;
            Panel[] pnls = new Panel[7];
            pnls[0] = panelConnect;
            pnls[1] = panelCapture;
            pnls[2] = panelParam;
            pnls[3] = panelGPS;
            pnls[4] = panelCFW;
            pnls[5] = panelCooler;
            pnls[6] = panelBurst;

            int x = i;
            int num = 7;

            if (show == 0)
            {
                btns[x].BackgroundImage = SdkDemo08.Properties.Resources.unfoldHead;
                pnls[x].Visible = false;
            }
            else
            {
                btns[x].BackgroundImage = SdkDemo08.Properties.Resources.foldHead;
                pnls[x].Visible = true;
            }

            for (; x < num - 1; x++)
            {
                if (pnls[x].Visible == false)
                {
                    btns[x + 1].Location = new Point(btns[x + 1].Location.X, btns[x].Location.Y + btns[x].Height + 6);
                    pnls[x + 1].Location = new Point(pnls[x + 1].Location.X, btns[x + 1].Location.Y + btns[x + 1].Height + 6);
                }
                else
                {
                    pnls[x].Location = new Point(pnls[x].Location.X, btns[x].Location.Y + btns[x].Height + 6);

                    btns[x + 1].Location = new Point(btns[x + 1].Location.X, pnls[x].Location.Y + pnls[x].Height + 6);
                    pnls[x + 1].Location = new Point(pnls[x + 1].Location.X, btns[x + 1].Location.Y + btns[x + 1].Height + 6);
                }
            }

            if (x == num)
            {
                pnls[x].Location = new Point(pnls[x].Location.X, btns[x].Location.Y + btns[x].Height + 6);
            }
        }

        private void btnHeadConnect_Click(object sender, EventArgs e)
        {
            if (b1 == 1)
            {
                b1 = b1 * (-1);
                UpdateLocation(0, 0);
            }
            else
            {
                b1 = b1 * (-1);
                UpdateLocation(0, 1);
            }
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.Description = "Seleccione la carpeta donde se guardarán las imágenes FITS";
            folderBrowserDialog.ShowNewFolderButton = true;
            
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                saveFolderPath = folderBrowserDialog.SelectedPath;
                string folderName = Path.GetFileName(saveFolderPath);
                if (folderName.Length > 30)
                {
                    folderName = folderName.Substring(0, 27) + "...";
                }
                labelSaveFolder.Text = folderName;
                Console.WriteLine("Carpeta de guardado seleccionada: {0}", saveFolderPath);
            }
        }

        private void btnHeadCapture_Click(object sender, EventArgs e)
        {
            if (b2 == 1)
            {
                b2 = b2 * (-1);
                UpdateLocation(1, 0);
            }
            else
            {
                b2 = b2 * (-1);
                UpdateLocation(1, 1);
            }
        }

        private void btnHeadParam_Click(object sender, EventArgs e)
        {
            if (b3 == 1)
            {
                b3 = b3 * (-1);
                UpdateLocation(2, 0);
            }
            else
            {
                b3 = b3 * (-1);
                UpdateLocation(2, 1);
            }
        }

        private void btnHeadGPS_Click(object sender, EventArgs e)
        {
            if (b4 == 1)
            {
                b4 = b4 * (-1);
                UpdateLocation(3, 0);
            }
            else
            {
                b4 = b4 * (-1);
                UpdateLocation(3, 1);
            }
        }

        private void btnHeadCFW_Click(object sender, EventArgs e)
        {
            if (b5 == 1)
            {
                b5 = b5 * (-1);
                UpdateLocation(4, 0);
            }
            else
            {
                b5 = b5 * (-1);
                UpdateLocation(4, 1);
            }
        }

        private void btnHeadCooler_Click(object sender, EventArgs e)
        {
            if (b6 == 1)
            {
                b6 = b6 * (-1);
                UpdateLocation(5, 0);
            }
            else
            {
                b6 = b6 * (-1);
                UpdateLocation(5, 1);
            }
        }

        private void btnHeadBurst_Click(object sender, EventArgs e)
        {
            if (b7 == 1)
            {
                b7 = b7 * (-1);
                UpdateLocation(6, 0);
            }
            else
            {
                b7 = b7 * (-1);
                UpdateLocation(6, 1);
            }
        }

        //#region Common properties and methods.

        private void Connection_Click(object sender, EventArgs e)
        {
            isSetupUI = true;

            InitRegisterPnpEventIn();
            InitRegisterPnpEventOut();

            /*************************************************************************************************/
            /******************************** Initizlize and Get Camera handle *******************************/
            /*************************************************************************************************/
            retVal = ASCOM.QHYCCD.libqhyccd.InitQHYCCDResource();
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Initializ resource failed.");
            }


            camScanNum = ASCOM.QHYCCD.libqhyccd.ScanQHYCCD();
            if (camScanNum <= 0)
            {
                DialogResult dr = MessageBox.Show("No camera be detected.");
                goto end;
            }

            retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDId(0, Common.camID);
            Console.WriteLine("Cam ID = {0}", Common.camID);
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Get device ID failed.");
                goto end;
            }
            Common.camHandle = ASCOM.QHYCCD.libqhyccd.OpenQHYCCD(Common.camID);
            if (Common.camHandle == null)
            {
                DialogResult dr = MessageBox.Show("Open device failed.");
                goto end;
            }

            /*************************************************************************************************/
            /*************************************** Set Camera ReadMode *************************************/
            /*************************************************************************************************/
            //isSetupUI = true;

            this.comBoxReadMode.Items.Clear();

            uint readModeWidth = 0, readModeHeight = 0, readModeNum = 0;
            byte[] readModeName = new byte[100];
            retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDNumberOfReadModes(Common.camHandle, ref readModeNum);
            Console.WriteLine("Read Mode number : {0}", readModeNum);

            for (uint i = 0; i < readModeNum; i++)
            {
                retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDReadModeName(Common.camHandle, i, readModeName);
                if (retVal == 0)
                {
                    Console.WriteLine("Read Mode name : {0}", readModeName);

                    string strGet = System.Text.Encoding.Default.GetString(readModeName, 0, readModeName.Length);
                    this.comBoxReadMode.Items.Add(strGet);

                    retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDReadModeResolution(Common.camHandle, i, ref readModeWidth, ref readModeHeight);
                    Console.WriteLine("Read Mode resolution : {0} X {1}", readModeWidth, readModeHeight);
                    if (Common.camMaxResolution < readModeWidth * readModeHeight)
                    {
                        Common.camMaxImageWidth  = readModeWidth;
                        Common.camMaxImageHeight = readModeHeight;
                        Common.camMaxResolution  = readModeWidth * readModeHeight;
                    }
                }
            }

            this.comBoxReadMode.SelectedIndex = 0;
            Common.camReadMode = 0;
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);
            ASCOM.QHYCCD.libqhyccd.GetQHYCCDReadModeResolution(Common.camHandle, Common.camReadMode, ref readModeWidth, ref readModeHeight);
            if (Common.camMinResolution < readModeWidth * readModeHeight)
            {
                Common.camMinImageWidth  = readModeWidth;
                Common.camMinImageHeight = readModeHeight;
                Common.camMinResolution  = readModeWidth * readModeHeight;
            }

            if (readModeNum <= 1)
            {
                this.label10.Enabled = false;
                this.comBoxReadMode.Enabled = false;
            }
            else
            {
                this.label10.Enabled = true;
                this.comBoxReadMode.Enabled = true;
            }

            // Habilitar selector de formato de archivo
            this.labelFileFormat.Enabled = true;
            this.comBoxFileFormat.Enabled = true;
            Common.imageFileFormat = this.comBoxFileFormat.SelectedItem != null ? this.comBoxFileFormat.SelectedItem.ToString() : "FITS";

            //isSetupUI = false;

            /*************************************************************************************************/
            /************************************** Set Camera StreamMode ************************************/
            /*************************************************************************************************/
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_LIVEVIDEOMODE);
            if (retVal == 0)
            {
                Common.canLive = true;
            }
            else
            {
                Common.canLive = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_SINGLEFRAMEMODE);
            if (retVal == 0)
            {
                Common.canSingle = true;
            }
            else
            {
                Common.canSingle = false;
            }
            Console.WriteLine("canLive = {0} canSingle = {1}", Common.canLive, Common.canSingle);

            
            if (this.radioBtnSingle.Checked == true) //勾选单帧模式
            {
                Common.camStreamMode = 0;
            }
            else //勾选连续模式
            {
                if (Common.canLive)
                    Common.camStreamMode = 1;
                else
                    Common.camStreamMode = 0;
            }
            
            Console.WriteLine("Stream Mode = {0}", Common.camStreamMode);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Set mode failed.");
                goto end;
            }
            // Los radio buttons se mantienen habilitados para permitir cambio de modo mientras la cámara está conectada
            // this.radioBtnSingle.Enabled = false;
            // this.radioBtnLive.Enabled = false;


            /*************************************************************************************************/
            /********************************** Initizlize Camera Parameters *********************************/
            /*************************************************************************************************/
            retVal = ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Initialize device failed.");
                goto end;
            }

            ///**************************************************************************************************/
            ///************************************** Initialize Software UI ************************************/
            ///**************************************************************************************************/

            ///
            /// Get Camera Info
            /// 
            byte[] bufFW = new byte[32];
            byte[] bufFPGA1 = new byte[32];
            byte[] bufFPGA2 = new byte[32];
            ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDFWVersion(Common.camHandle, bufFW);
            ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDFPGAVersion(Common.camHandle, 0, bufFPGA1);
            ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDFPGAVersion(Common.camHandle, 1, bufFPGA2);

            retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDChipInfo(Common.camHandle, ref Common.camChipWidth, ref Common.camChipHeight, ref Common.camImageWidth, ref Common.camImageHeight, ref Common.camPixelWidth, ref Common.camPixelHeight, ref Common.camImageBits);
            Console.WriteLine("w = {0} h = {1}", Common.camMaxImageWidth, Common.camMaxImageHeight);
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Get device chip information failed.");
                goto end;
            }
            else
            {
                if (Common.camMinResolution < Common.camImageWidth * Common.camImageHeight)
                {
                    Common.camMinImageWidth  = Common.camImageWidth;
                    Common.camMinImageHeight = Common.camImageHeight;
                    Common.camMinResolution  = Common.camMinImageWidth * Common.camMinImageHeight;
                }
            }
            retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
            Console.WriteLine("Effective Area x = {0} y = {1} w = {2} h = {3}", Common.camEFStartX, Common.camEFStartY, Common.camEFSizeX, Common.camEFSizeY);
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Get device chip information failed.");
                goto end;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDOverScanArea(Common.camHandle, ref Common.camOSStartX, ref Common.camOSStartY, ref Common.camOSSizeX, ref Common.camOSSizeY);
            Console.WriteLine("Overscan Area x = {0} y = {1} w = {2} h = {3}", Common.camOSStartX, Common.camOSStartY, Common.camOSSizeX, Common.camOSSizeY);
            if (retVal != 0)
            {
                DialogResult dr = MessageBox.Show("Get device chip information failed.");
                goto end;
            }

            if (Common.camOSSizeX != 0 && Common.camOSSizeY != 0)
            {
                Common.canIgoreOS = true;

                if (this.radioBtnSingle.Checked == true)
                    this.checkBoxOS.Enabled = true;
                else
                    this.checkBoxOS.Enabled = false;
            }

            uint year = 0, month = 0, day = 0, subday = 0;
            retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDSDKVersion(ref year, ref month, ref day, ref subday);

            this.labelCamID.Text = Common.camID.ToString();
            if ((bufFW[0] >> 4) <= 9)
                this.labelFW.Text   = ((bufFW[0] >> 4) + 0x10).ToString() + "-" + (bufFW[0] & ~0xF0).ToString() + "-" + bufFW[1].ToString();
            else
                this.labelFW.Text   = bufFW[0].ToString() + "-" + (bufFW[0] & ~0xF0).ToString() + "-" + bufFW[1].ToString();
            this.labelFPGA1.Text    = bufFPGA1[0].ToString() + "-" + bufFPGA1[1].ToString() + "-" + bufFPGA1[2].ToString();
            this.labelFPGA2.Text    = bufFPGA2[0].ToString() + "-" + bufFPGA2[1].ToString() + "-" + bufFPGA2[2].ToString();
            this.labelSDK.Text      = year.ToString() + "-" + month.ToString() + "-" + day.ToString();// +"-" + subday.ToString();
            this.labelMaxImage.Text = Common.camMaxImageWidth.ToString() + " X " + Common.camMaxImageHeight.ToString();
            this.labelChip.Text     = Common.camChipWidth.ToString() + " X " + Common.camChipHeight.ToString() + " mm";
            this.labelPixel.Text    = Common.camPixelWidth.ToString() + " X " + Common.camPixelHeight.ToString() + " um";

            ///
            /// BIN
            ///

            this.comBoxBinMode.Items.Clear();

            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_BIN1X1MODE);
            if (retVal == 0)
            {
                Common.canBIN1X1 = true;
                this.comBoxBinMode.Enabled = false;
                this.comBoxBinMode.Items.Add("1X1");
            }
            else
            {
                Common.canBIN1X1 = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_BIN2X2MODE);
            if (retVal == 0)
            {
                Common.canBIN2X2 = true;
                this.label22.Enabled = true;
                this.comBoxBinMode.Enabled = true;
                this.comBoxBinMode.Items.Add("2X2");
            }
            else
            {
                Common.canBIN2X2 = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_BIN3X3MODE);
            if (retVal == 0)
            {
                Common.canBIN3X3 = true;
                this.label22.Enabled = true;
                this.comBoxBinMode.Enabled = true;
                this.comBoxBinMode.Items.Add("3X3");
            }
            else
            {
                Common.canBIN3X3 = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_BIN4X4MODE);
            if (retVal == 0)
            {
                Common.canBIN4X4 = true;
                this.label22.Enabled = true;
                this.comBoxBinMode.Enabled = true;
                this.comBoxBinMode.Items.Add("4X4");
            }
            else
            {
                Common.canBIN4X4 = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_BIN6X6MODE);
            if (retVal == 0)
            {
                Common.canBIN6X6 = true;
                this.label22.Enabled = true;
                this.comBoxBinMode.Enabled = true;
                this.comBoxBinMode.Items.Add("6X6");
            }
            else
            {
                Common.canBIN6X6 = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_BIN8X8MODE);
            if (retVal == 0)
            {
                Common.canBIN8X8 = true;
                this.label22.Enabled = true;
                this.comBoxBinMode.Enabled = true;
                this.comBoxBinMode.Items.Add("8X8");
            }
            else
            {
                Common.canBIN8X8 = false;
            }
            Console.WriteLine("canBIN1X1 = {0}", Common.canBIN1X1);
            Console.WriteLine("canBIN2X2 = {0}", Common.canBIN2X2);
            Console.WriteLine("canBIN3X3 = {0}", Common.canBIN3X3);
            Console.WriteLine("canBIN4X4 = {0}", Common.canBIN4X4);
            Console.WriteLine("canBIN6X6 = {0}", Common.canBIN6X6);
            Console.WriteLine("canBIN8X8 = {0}", Common.canBIN8X8);

            this.comBoxBinMode.SelectedIndex = 0;
            Common.camBinX = 1;
            Common.camBinY = 1;
            Common.camImageStartX = 0;
            Common.camImageStartY = 0;
            Common.camImageSizeX  = Common.camMinImageWidth;
            Common.camImageSizeY  = Common.camMinImageHeight;
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageWidth, Common.camImageHeight);

            ///
            /// Bits & Color
            ///

            this.comBoxBits.Items.Clear();

            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_8BITS);
            if (retVal == 0)
            {
                Common.canSet8Bits = true;
            }
            else
            {
                Common.canSet8Bits = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_16BITS);
            if (retVal == 0)
            {
                Common.canSet16Bits = true;
            }
            else
            {
                Common.canSet16Bits = false;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_IS_COLOR);
            if (retVal == 0)
            {
                Common.canSetColor = true;
            }
            else
            {
                Common.canSetColor = false;
            }

            if (this.radioBtnLive.Checked)
            {
                if (Common.canLive)
                {
                    this.comBoxBits.Items.Clear();

                    if (Common.canSet8Bits)
                    {
                        this.comBoxBits.Items.Add("RAW8");
                    }
                    if (Common.canSet16Bits)
                    {
                        this.comBoxBits.Items.Add("RAW16");
                    }
                    if (Common.canSetColor)
                    {
                        this.comBoxBits.Items.Add("RGB24");
                    }

                    this.comBoxBits.SelectedIndex = 0;
                    this.label9.Enabled = true;
                    this.comBoxBits.Enabled = true;
                    Common.camImageBits = 8;
                    Common.camColorOnOff = false;
                }
                else
                {
                    this.comBoxBits.Items.Clear();
                    this.comBoxBits.Items.Add("RAW16");
                    this.comBoxBits.SelectedIndex = 0;
                    this.label9.Enabled = false;
                    this.comBoxBits.Enabled = false;
                    Common.camImageBits = 16;
                    Common.camColorOnOff = false;
                }
            }
            else
            {
                this.comBoxBits.Items.Clear();
                this.comBoxBits.Items.Add("RAW16");
                this.comBoxBits.SelectedIndex = 0;
                this.label9.Enabled = true;
                this.comBoxBits.Enabled = false;
                Common.camImageBits = 16;
                Common.camColorOnOff = false;
            }

            Console.WriteLine("canSet8Bits = {0} canSet16Bits = {1} canSetColor = {2} camImageBits = {3} camColorOnOff = {4}", Common.canSet8Bits, Common.canSet16Bits, Common.canSetColor, Common.camImageBits, Common.camColorOnOff);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

            isConnect = true;

            //Expose Time
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE);
            if (retVal == 0)
            {
                this.label1.Enabled      = true;
                this.trackBarExp.Enabled = true;
                this.textBoxExp.Enabled  = true;
                this.comBoxUnit.Enabled  = true;
                this.comBoxUnit.Items.Clear();
                this.comBoxUnit.Items.Add("1~1000 us");
                this.comBoxUnit.Items.Add("1~1000 ms");
                this.comBoxUnit.Items.Add("1~1000 s");

                Common.camExpTime = 20000f;
                this.trackBarExp.Value = 20;
                this.textBoxExp.Text = "20";
                this.comBoxUnit.SelectedIndex = 1;
            }
            else
            {
                this.label1.Enabled = false;
                this.trackBarExp.Enabled = false;
                this.textBoxExp.Enabled = false;
            }
            Console.WriteLine("ExpTime = {0}", Common.camExpTime);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

            //Gain
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_GAIN);
            if (retVal == 0)
            {
                Common.canSetGain = true;
                this.label3.Enabled = true;
                this.trackBarGain.Enabled = true;
                this.testBoxGain.Enabled = true;

                double minGain = 0, maxGain = 0, stepGain = 0;
                retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParamMinMaxStep(Common.camHandle, CONTROL_ID.CONTROL_GAIN, ref minGain, ref maxGain, ref stepGain);
                if (retVal == 0)
                {
                    this.trackBarGain.Minimum = (int)minGain;
                    this.trackBarGain.Maximum = (int)maxGain;
                }

                Common.camGain = 1;
            }
            else
            {
                Common.canSetGain = false;
                this.label3.Enabled = false;
                this.trackBarGain.Enabled = false;
                this.testBoxGain.Enabled = false;
            }
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);

            //Offset
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_OFFSET);
            if (retVal == 0)
            {
                Common.canSetOffset = true;
                this.label2.Enabled = true;
                this.trackBarOffset.Enabled = true;
                this.textBoxOffset.Enabled = true;

                double minOffset = 0, maxOffset = 0, stepOffset = 0;
                retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParamMinMaxStep(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, ref minOffset, ref maxOffset, ref stepOffset);
                if (retVal == 0)
                {
                    this.trackBarOffset.Minimum = (int)minOffset;
                    this.trackBarOffset.Maximum = (int)maxOffset;
                }

                Common.camOffset = 1;
            }
            else
            {
                Common.canSetOffset = false;
                this.label2.Enabled = false;
                this.trackBarOffset.Enabled = false;
                this.textBoxOffset.Enabled = false;
            }
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);

            //Traffic
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC);
            if (retVal == 0)
            {
                Common.canSetTraffic = true;
                this.label4.Enabled = true;
                this.trackBarUSBTraffic.Enabled = true;
                this.textBoxUSBTraffic.Enabled = true;

                double minTraffic = 0, maxTraffic = 0, stepTraffic = 0;
                retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParamMinMaxStep(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, ref minTraffic, ref maxTraffic, ref stepTraffic);
                if (retVal == 0)
                {
                    this.trackBarUSBTraffic.Minimum = (int)minTraffic;
                    this.trackBarUSBTraffic.Maximum = (int)maxTraffic;
                }

                Common.camTraffic = 30;
            }
            else
            {
                Common.canSetTraffic = false;
                this.label4.Enabled = false;
                this.trackBarUSBTraffic.Enabled = false;
                this.textBoxUSBTraffic.Enabled = false;
            }
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_DDR, 1.0);

            //Speed
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_SPEED);
            if (retVal == 0)
            {
                Common.canSetSpeed = true;
            }
            else
            {
                Common.canSetSpeed = false;
            }

            //Cooler
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_COOLER);
            if (retVal == 0)
            {
                Common.canCooler = true;
                this.panelCooler.Enabled = true;

                this.label8.Enabled = true;
                this.labelNowTemp.Enabled = true;
                this.label20.Enabled = true;
                this.labelNowPWM.Enabled = true;
            }
            else
            {
                Common.canCooler = false;
                this.panelCooler.Enabled = false;
            }
            //Humidity
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_HUMIDITY);
            if (retVal == 0)
            {
                Common.canHumidity = true;
                this.label11.Enabled = true;
                this.labelHumidity.Enabled = true;
            }
            else
            {
                Common.canHumidity = false;
                this.label11.Enabled = false;
                this.labelHumidity.Enabled = false;
            }
            //Pressure
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_PRESSURE);
            if (retVal == 0)
            {
                Common.canPressure = true;
                this.label17.Enabled = true;
                this.labelPressure.Enabled = true;
            }
            else
            {
                Common.canPressure = false;
                this.label17.Enabled = false;
                this.labelPressure.Enabled = false;
            }

            //CFW
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT);
            //retVal = -1;
            if (retVal == 0)
            {
                Common.canSetCFW = true;

                retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDCFWPlugged(Common.camHandle);
                if (retVal == 0)
                {
                    this.panelCFW.Enabled = true;
                    this.label16.Enabled = true;
                    this.comBoxCFWPos.Enabled = true;
                    this.label28.Enabled = true;
                    this.labelCFWStatus.Enabled = true;

                    double num = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWSLOTSNUM);
                    Console.WriteLine("num = {0}", num);
                    if (num > 1)
                    {
                        isInitCFW = true;

                        for (int i = 0; i < (int)num; i++)
                        {
                            this.comBoxCFWPos.Items.Add(cfwNames[i]);
                        }

                        this.comBoxCFWPos.SelectedIndex = 0;

                        ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT, 48.0 + 0.0);

                        //curCFWPos = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT);
                        //while (curCFWPos != 48.0)
                        //{
                        //    Application.DoEvents();

                        //    curCFWPos = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT);
                        //}

                        isInitCFW = false;
                    }
                }
                else
                {
                    this.panelCFW.Enabled = false;
                    this.label16.Enabled = false;
                    this.comBoxCFWPos.Enabled = false;
                    this.label28.Enabled = false;
                    this.labelCFWStatus.Enabled = false;
                }
            }
            else
            {
                Common.canSetCFW = false;
                this.panelCFW.Enabled = false;
            }

            //GPS
            retVal = ASCOM.QHYCCD.libqhyccd.IsQHYCCDControlAvailable(Common.camHandle, CONTROL_ID.CAM_GPS);
            if (retVal == 0)
            {
                Common.canSetGPS = true;
                this.tabPageGPS.Parent = this.tabControlInfo;
                this.panelGPS.Enabled = true;
                this.btnLEDOnOff.Enabled = false;
                this.trackBarVCOX.Enabled = false;
                this.textBoxVCOX.Enabled = false;
                this.numUDPA1.Enabled = true;
                this.numUDPA2.Enabled = true;
                this.numUDPA3.Enabled = true;
                this.numUDPA4.Enabled = true;
                this.numUDPA5.Enabled = true;
                this.numUDPA6.Enabled = true;
                this.numUDPA7.Enabled = true;
                this.numUDPA8.Enabled = true;
                this.numUDPA9.Enabled = true;
                this.numUDPB1.Enabled = true;
                this.numUDPB2.Enabled = true;
                this.numUDPB3.Enabled = true;
                this.numUDPB4.Enabled = true;
                this.numUDPB5.Enabled = true;
                this.numUDPB6.Enabled = true;
                this.numUDPB7.Enabled = true;
                this.numUDPB8.Enabled = true;
                this.numUDPB9.Enabled = true;
            }
            else
            {
                this.tabPageGPS.Parent = null;
                Common.canSetGPS = false;
                this.panelGPS.Enabled = false;
            }

            //Create Memory
            Common.length = ASCOM.QHYCCD.libqhyccd.GetQHYCCDMemLength(Common.camHandle);
            if (Common.length <= 0)
            {
                DialogResult dr = MessageBox.Show("Get image memory length failed.");
                goto end;
            }
            rawArray = new byte[Common.camMaxImageWidth * Common.camMaxImageHeight * 4];

            //Cooler
            if (Common.canCooler)
            {
                quitCoolerGet = false;
                hasQuitCoolerGet = false;
                threadControlCooler = new Thread(new ParameterizedThreadStart(ControlCooler));
                threadControlCooler.Start();

                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_MANULPWM, 0.0);
            }

            this.Text = this.Text + " | Connected Camera : " + Common.camID.ToString();

            DialogResult dr1 = MessageBox.Show("Connect success.");

            // Mantener los radio buttons habilitados para permitir cambio de modo mientras la cámara está conectada
            // Se deshabilitarán solo durante la captura activa
            this.radioBtnLive.Enabled = true;
            this.radioBtnSingle.Enabled = true;
            this.radioBtnRecording.Enabled = true;

            this.Connection.Enabled = false;
            this.DisConnection.Enabled = true;

            // Configurar botones según el modo seleccionado
            if (Common.canLive && this.radioBtnLive.Checked)
            {
                // Modo Live: Solo preview, no captura
                this.btnStartCap.Enabled = false;
                this.btnStopCap.Enabled = false;
                
                this.btnBurstOnOff.Enabled     = true;
                this.btnBurstStartEnd.Enabled  = false;
                this.textBoxBurstStart.Enabled = false;
                this.textBoxBurstEnd.Enabled   = false;
                this.btnBurstPatch.Enabled     = false;
                this.textBoxBurstPatch.Enabled = false;
                this.btnBurstCapture.Enabled   = false;
                
                // Iniciar automáticamente el preview en modo Live
                quit = false;
                hasquit = false;
                
                // Verificar que rawArray tenga el tamaño correcto para la resolución actual
                // Usar el tamaño máximo para asegurar que sea suficiente
                if (rawArray == null || rawArray.Length < (Common.camMaxImageWidth * Common.camMaxImageHeight * 4))
                {
                    Console.WriteLine("Redimensionando rawArray: tamaño actual={0}, tamaño máximo={1}", 
                        rawArray != null ? rawArray.Length : 0, Common.camMaxImageWidth * Common.camMaxImageHeight * 4);
                    rawArray = new byte[Common.camMaxImageWidth * Common.camMaxImageHeight * 4];
                }
                
                uint beginResult = ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(Common.camHandle);
                Console.WriteLine("BeginQHYCCDLive retornó: {0}", beginResult);
                
                if (beginResult == 0)
                {
                    // Pequeño delay para que la cámara inicie el modo Live
                    System.Threading.Thread.Sleep(100);
                    
                    threadShowLiveImage = new Thread(new ParameterizedThreadStart(ShowLiveImage));
                    threadShowLiveImage.Start();
                    Console.WriteLine("Modo Live iniciado automáticamente después de conectar");
                }
                else
                {
                    Console.WriteLine("Error al iniciar modo Live: BeginQHYCCDLive retornó {0}", beginResult);
                }
            }
            else
            {
                // Modo Single: Permitir capturas
                this.btnStartCap.Enabled = true;
                this.btnStopCap.Enabled = false;
            }

            this.label1.Enabled = true;
            this.trackBarExp.Enabled = true;
            this.textBoxExp.Enabled = true;


            this.label5.Enabled = false;
            this.label6.Enabled = true;
            this.label7.Enabled = false;
            this.label9.Enabled = true;

            this.btnSlave.Enabled = false;
            this.trackBarTS.Enabled = false;
            this.trackBarTarget.Enabled = false;
            this.trackBarExpTime.Enabled = false;
            this.btnGPSOnOff.Enabled = true;

            isSetupUI = false;

        end: ;
        }

        private void DisConnection_Click(object sender, EventArgs e)
        {
            int ret = -1;
            
            // Detener cualquier captura o preview activo
            quit = true;
            while (hasquit != true)
            {
                Application.DoEvents();
            }
            
            // Detener threads de imagen
            if (threadShowLiveImage != null && threadShowLiveImage.IsAlive)
            {
                threadShowLiveImage.Abort();
            }
            if (threadShowSingleImage != null && threadShowSingleImage.IsAlive)
            {
                threadShowSingleImage.Abort();
            }
            
            // Detener modo Live si está activo
            if (Common.canLive && this.radioBtnLive.Checked)
            {
                ret = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);
            }
            else if (this.radioBtnSingle.Checked)
            {
                ret = ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);
            }

            if (Common.canCooler)
            {
                quitCoolerGet = true;
                while (hasQuitCoolerGet != true)
                {
                    Application.DoEvents();
                }
                threadControlCooler.Abort();
            }

            //quitGetStatus = true;
            //while (hasQuitGetStatus != true)
            //{
            //    Application.DoEvents();
            //}

            ret = ASCOM.QHYCCD.libqhyccd.CloseQHYCCD(Common.camHandle);
            if (ret != 0)
            {
                DialogResult dr = MessageBox.Show("Close device failed.");
                goto end;
            }

            ret = ASCOM.QHYCCD.libqhyccd.ReleaseQHYCCDResource();
            if (ret != 0)
            {
                DialogResult dr = MessageBox.Show("Release resource failed.");
                goto end;
            }

            if (Common.canLive && this.radioBtnLive.Checked)
            {
                this.btnBurstOnOff.Enabled     = false;
                this.btnBurstStartEnd.Enabled  = false;
                this.textBoxBurstStart.Enabled = false;
                this.textBoxBurstEnd.Enabled   = false;
                this.btnBurstPatch.Enabled     = false;
                this.textBoxBurstPatch.Enabled = false;
                this.btnBurstCapture.Enabled   = false;
            }

            this.Connection.Enabled = true;
            this.DisConnection.Enabled = false;

            // Los radio buttons se mantienen habilitados después de desconectar
            // (aunque no se usarán hasta que se conecte otra cámara)
            this.radioBtnLive.Enabled = true;
            this.radioBtnSingle.Enabled = true;
            this.radioBtnRecording.Enabled = true;

            this.labelCamID.Text = "-";
            this.labelFW.Text = "-";
            this.labelFPGA1.Text = "-";
            this.labelFPGA2.Text = "-";
            this.labelSDK.Text = "-";
            this.labelMaxImage.Text = "-";
            this.labelChip.Text = "-";
            this.labelPixel.Text = "-";

            this.comBoxUnit.Items.Clear();
            this.comBoxUnit.Enabled = false;

            this.tabPageGPS.Parent = null;

            this.label16.Enabled = false;
            this.comBoxCFWPos.Enabled = false;

            this.btnStartCap.Enabled = false;
            this.btnStopCap.Enabled = false;

            this.checkBoxOS.Enabled = false;

            this.label10.Enabled = false;
            this.comBoxReadMode.Enabled = false;
            this.comBoxReadMode.Items.Clear();

            this.label9.Enabled = false;
            this.comBoxBits.Enabled = false;
            this.comBoxBits.Items.Clear();

            if (this.labelFileFormat != null)
            {
                this.labelFileFormat.Enabled = false;
            }
            if (this.comBoxFileFormat != null)
            {
                this.comBoxFileFormat.Enabled = false;
            }

            this.label22.Enabled = false;
            this.comBoxBinMode.Enabled = false;
            this.comBoxBinMode.Items.Clear();

            this.trackBarExp.Enabled = false;
            this.textBoxExp.Enabled = false;
            this.trackBarGain.Enabled = false;
            this.testBoxGain.Enabled = false;
            this.trackBarOffset.Enabled = false;
            this.textBoxOffset.Enabled = false;
            this.trackBarUSBTraffic.Enabled = false;
            this.textBoxUSBTraffic.Enabled = false;

            this.btnLEDOnOff.Enabled = false;
            this.trackBarVCOX.Enabled = false;
            this.textBoxVCOX.Enabled = false;
            this.numUDPA1.Enabled = false;
            this.numUDPA2.Enabled = false;
            this.numUDPA3.Enabled = false;
            this.numUDPA4.Enabled = false;
            this.numUDPA5.Enabled = false;
            this.numUDPA6.Enabled = false;
            this.numUDPA7.Enabled = false;
            this.numUDPA8.Enabled = false;
            this.numUDPA9.Enabled = false;
            this.numUDPB1.Enabled = false;
            this.numUDPB2.Enabled = false;
            this.numUDPB3.Enabled = false;
            this.numUDPB4.Enabled = false;
            this.numUDPB5.Enabled = false;
            this.numUDPB6.Enabled = false;
            this.numUDPB7.Enabled = false;
            this.numUDPB8.Enabled = false;
            this.numUDPB9.Enabled = false;

            this.label1.Enabled = false;
            this.label2.Enabled = false;
            this.label3.Enabled = false;
            this.label4.Enabled = false;
            this.label5.Enabled = false;
            this.label6.Enabled = false;
            this.label7.Enabled = false;
            this.label9.Enabled = false;
            this.label10.Enabled = false;

            this.Text = "C# Demo for GPS";

            DialogResult dr1 = MessageBox.Show("Disconnect success.");

        end: ;
        }

        

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private static System.Windows.Media.PixelFormat ConvertBmpPixelFormat(System.Drawing.Imaging.PixelFormat pixelformat)
        {
            System.Windows.Media.PixelFormat pixelFormats = System.Windows.Media.PixelFormats.Default;

            switch (pixelformat)
            {
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    pixelFormats = System.Windows.Media.PixelFormats.Bgr32;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    pixelFormats = System.Windows.Media.PixelFormats.Gray8;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                    pixelFormats = System.Windows.Media.PixelFormats.Gray16;
                    break;
            }

            return pixelFormats;
        }

        /******************************************************************/
        /***************************** Capture ****************************/
        /******************************************************************/
        
        /// <summary>
        /// Guarda una imagen en el formato seleccionado (FITS o PNG)
        /// </summary>
        unsafe private void SaveImageToFITS(byte[] rawData, uint width, uint height, 
            uint bitsPerPixel, uint channels, string imageType, uint frameNumber = 0, string filePath = null)
        {
            // Usar el formato seleccionado
            string format = Common.imageFileFormat;
            if (this.comBoxFileFormat != null && this.comBoxFileFormat.SelectedItem != null)
            {
                format = this.comBoxFileFormat.SelectedItem.ToString();
            }
            
            if (format == "PNG")
            {
                SaveImageToPNG(rawData, width, height, bitsPerPixel, channels, imageType, frameNumber, filePath);
                return;
            }
            
            // Por defecto usar FITS
            SaveImageToFITSInternal(rawData, width, height, bitsPerPixel, channels, imageType, frameNumber, filePath);
        }

        /// <summary>
        /// Guarda una imagen en formato FITS con los datos RAW
        /// </summary>
        unsafe private void SaveImageToFITSInternal(byte[] rawData, uint width, uint height, 
            uint bitsPerPixel, uint channels, string imageType, uint frameNumber = 0, string filePath = null)
        {
            try
            {
                string finalFilePath = filePath;
                
                // Si no se proporciona filePath, calcularlo
                if (string.IsNullOrEmpty(finalFilePath))
                {
                    // Verificar que hay una carpeta seleccionada
                    if (string.IsNullOrEmpty(saveFolderPath))
                    {
                        Console.WriteLine("No se ha seleccionado carpeta de guardado.");
                        return;
                    }

                    // Determinar el nombre del archivo y la carpeta
                    string fileName;
                    string folderPath;
                    
                    if (imageType == "LIVE")
                    {
                        if (string.IsNullOrEmpty(currentLiveSessionFolder))
                        {
                            Console.WriteLine("No hay carpeta de sesión activa para Live mode.");
                            return;
                        }
                        folderPath = currentLiveSessionFolder;
                        DateTime now = DateTime.UtcNow;
                        fileName = string.Format("Live_{0}_{1:D3}.fit", 
                            now.ToString("yyyyMMdd_HHmmss"), frameNumber);
                    }
                    else if (imageType == "RECORDING")
                    {
                        folderPath = recordingSessionFolder;
                        fileName = string.Format("{0}_{1:D4}.fit", 
                            recordingConfig.DestinationName, frameNumber);
                    }
                    else // SINGLE
                    {
                        folderPath = saveFolderPath;
                        
                        // Si es la primera captura de la sesión, buscar el último número usado
                        if (singleFrameCounter == 0)
                        {
                            singleFrameCounter = GetLastSingleFrameNumber(folderPath);
                        }
                        
                        singleFrameCounter++;
                        DateTime now = DateTime.UtcNow;
                        fileName = string.Format("Single_{0}_{1:D4}.fit", 
                            now.ToString("yyyyMMdd_HHmmss"), singleFrameCounter);
                    }

                    finalFilePath = Path.Combine(folderPath, fileName);
                }

                // Extraer solo los datos RAW necesarios (sin los 44 bytes de GPS si están presentes)
                int rawDataSize = (int)(width * height * channels * (bitsPerPixel / 8));
                byte[] rawImageData = new byte[rawDataSize];
                
                // Si hay GPS activo (btnGPSOnOff.Text == "OFF" significa GPS activo en este código),
                // los primeros 44 bytes son metadata GPS
                int offset = 0;
                if (this.btnGPSOnOff != null && this.btnGPSOnOff.Text == "OFF" && rawData.Length > rawDataSize + 44)
                {
                    offset = 44;
                }
                
                // Verificar que tenemos suficientes datos
                if (offset + rawDataSize <= rawData.Length)
                {
                    Array.Copy(rawData, offset, rawImageData, 0, rawDataSize);
                }
                else
                {
                    // Si no hay suficientes datos, usar lo que esté disponible
                    int availableSize = Math.Min(rawDataSize, rawData.Length - offset);
                    if (availableSize > 0)
                    {
                        Array.Copy(rawData, offset, rawImageData, 0, availableSize);
                        // Rellenar el resto con ceros si es necesario
                        for (int i = availableSize; i < rawDataSize; i++)
                        {
                            rawImageData[i] = 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Advertencia: No hay suficientes datos RAW para la imagen.");
                        return;
                    }
                }

                // Crear metadata FITS
                FITSMetadata metadata = new FITSMetadata();
                metadata.CameraModel = Common.camID.ToString();
                metadata.CameraID = Common.camID.ToString();
                metadata.ObservationDate = DateTime.UtcNow;
                metadata.ExposureTime = Common.camExpTime / 1000000.0; // Convertir de microsegundos a segundos
                metadata.Gain = Common.camGain;
                metadata.Offset = Common.camOffset;
                metadata.USBTraffic = Common.camTraffic;
                metadata.ImageType = imageType;
                metadata.BinX = Common.camBinX;
                metadata.BinY = Common.camBinY;
                metadata.ChipWidth = Common.camChipWidth;
                metadata.ChipHeight = Common.camChipHeight;
                metadata.PixelWidth = Common.camPixelWidth;
                metadata.PixelHeight = Common.camPixelHeight;
                metadata.FrameNumber = frameNumber;
                
                // Obtener temperatura si está disponible
                if (Common.canCooler)
                {
                    try
                    {
                        double temp = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CURTEMP);
                        metadata.Temperature = temp;
                    }
                    catch
                    {
                        // Si no se puede obtener, dejar el valor por defecto
                    }
                }

                // Obtener GPS data si está disponible
                if (this.btnGPSOnOff.Text == "OFF" && rawData.Length > 44)
                {
                    try
                    {
                        // Parsear GPS data de los primeros 44 bytes si es necesario
                        // Por ahora dejamos los valores por defecto
                    }
                    catch
                    {
                        // Si no se puede parsear, dejar valores por defecto
                    }
                }

                // Obtener read mode name si está disponible
                if (this.comBoxReadMode != null && this.comBoxReadMode.SelectedItem != null)
                {
                    metadata.ReadMode = this.comBoxReadMode.SelectedItem.ToString();
                }

                // Guardar archivo FITS
                FITSWriter.WriteFITS(finalFilePath, rawImageData, width, height, bitsPerPixel, channels, metadata);
                
                Console.WriteLine("Imagen guardada: {0}", finalFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar imagen FITS: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Guarda una imagen en formato PNG con ajuste automático y conversión RGB
        /// </summary>
        unsafe private void SaveImageToPNG(byte[] rawData, uint width, uint height, 
            uint bitsPerPixel, uint channels, string imageType, uint frameNumber = 0, string filePath = null)
        {
            try
            {
                string finalFilePath = filePath;
                
                // Si no se proporciona filePath, calcularlo
                if (string.IsNullOrEmpty(finalFilePath))
                {
                    // Verificar que hay una carpeta seleccionada
                    if (string.IsNullOrEmpty(saveFolderPath))
                    {
                        Console.WriteLine("No se ha seleccionado carpeta de guardado.");
                        return;
                    }

                    // Determinar el nombre del archivo y la carpeta
                    string fileName;
                    string folderPath;
                    
                    if (imageType == "LIVE")
                    {
                        if (string.IsNullOrEmpty(currentLiveSessionFolder))
                        {
                            Console.WriteLine("No hay carpeta de sesión activa para Live mode.");
                            return;
                        }
                        folderPath = currentLiveSessionFolder;
                        DateTime now = DateTime.UtcNow;
                        fileName = string.Format("Live_{0}_{1:D3}.png", 
                            now.ToString("yyyyMMdd_HHmmss"), frameNumber);
                    }
                    else if (imageType == "RECORDING")
                    {
                        folderPath = recordingSessionFolder;
                        fileName = string.Format("{0}_{1:D4}.png", 
                            recordingConfig.DestinationName, frameNumber);
                    }
                    else // SINGLE
                    {
                        folderPath = saveFolderPath;
                        
                        // Si es la primera captura de la sesión, buscar el último número usado
                        if (singleFrameCounter == 0)
                        {
                            singleFrameCounter = GetLastSingleFrameNumber(folderPath);
                        }
                        
                        singleFrameCounter++;
                        DateTime now = DateTime.UtcNow;
                        fileName = string.Format("Single_{0}_{1:D4}.png", 
                            now.ToString("yyyyMMdd_HHmmss"), singleFrameCounter);
                    }

                    finalFilePath = Path.Combine(folderPath, fileName);
                }

                // Extraer solo los datos RAW necesarios (sin los 44 bytes de GPS si están presentes)
                int rawDataSize = (int)(width * height * channels * (bitsPerPixel / 8));
                byte[] rawImageData = new byte[rawDataSize];
                
                // Si hay GPS activo, los primeros 44 bytes son metadata GPS
                int offset = 0;
                if (this.btnGPSOnOff != null && this.btnGPSOnOff.Text == "OFF" && rawData.Length > rawDataSize + 44)
                {
                    offset = 44;
                }
                
                // Verificar que tenemos suficientes datos
                if (offset + rawDataSize <= rawData.Length)
                {
                    Array.Copy(rawData, offset, rawImageData, 0, rawDataSize);
                }
                else
                {
                    int availableSize = Math.Min(rawDataSize, rawData.Length - offset);
                    if (availableSize > 0)
                    {
                        Array.Copy(rawData, offset, rawImageData, 0, availableSize);
                        for (int i = availableSize; i < rawDataSize; i++)
                        {
                            rawImageData[i] = 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Advertencia: No hay suficientes datos RAW para la imagen.");
                        return;
                    }
                }

                // Convertir RAW a RGB con ajuste automático
                Bitmap bitmap = ConvertRawToBitmap(rawImageData, width, height, bitsPerPixel, channels);
                
                if (bitmap != null)
                {
                    // Guardar como PNG
                    bitmap.Save(finalFilePath, ImageFormat.Png);
                    bitmap.Dispose();
                    Console.WriteLine("Imagen PNG guardada: {0}", finalFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar imagen PNG: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Convierte datos RAW a Bitmap RGB con ajuste automático (stretch)
        /// </summary>
        private Bitmap ConvertRawToBitmap(byte[] rawData, uint width, uint height, uint bitsPerPixel, uint channels)
        {
            try
            {
                Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);
                Rectangle rect = new Rectangle(0, 0, (int)width, (int)height);
                BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                
                IntPtr ptr = bmpData.Scan0;
                int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];
                
                // Encontrar min y max para ajuste automático
                int min = int.MaxValue;
                int max = int.MinValue;
                int pixelCount = (int)(width * height);
                
                if (bitsPerPixel == 8)
                {
                    if (channels == 1) // Monocromo
                    {
                        for (int i = 0; i < pixelCount; i++)
                        {
                            int val = rawData[i];
                            if (val < min) min = val;
                            if (val > max) max = val;
                        }
                        
                        // Aplicar ajuste automático (stretch)
                        double scale = (max > min) ? 255.0 / (max - min) : 1.0;
                        int index = 0;
                        for (int y = 0; y < height; y++)
                        {
                            int rgbIndex = y * bmpData.Stride;
                            for (int x = 0; x < width; x++)
                            {
                                int rawVal = rawData[index++];
                                int stretched = (int)((rawVal - min) * scale);
                                if (stretched < 0) stretched = 0;
                                if (stretched > 255) stretched = 255;
                                
                                rgbValues[rgbIndex + x * 3] = (byte)stretched;     // B
                                rgbValues[rgbIndex + x * 3 + 1] = (byte)stretched; // G
                                rgbValues[rgbIndex + x * 3 + 2] = (byte)stretched; // R
                            }
                        }
                    }
                    else if (channels == 3) // Color RGB
                    {
                        for (int i = 0; i < pixelCount * 3; i++)
                        {
                            int val = rawData[i];
                            if (val < min) min = val;
                            if (val > max) max = val;
                        }
                        
                        double scale = (max > min) ? 255.0 / (max - min) : 1.0;
                        int rawIndex = 0;
                        for (int y = 0; y < height; y++)
                        {
                            int rgbIndex = y * bmpData.Stride;
                            for (int x = 0; x < width; x++)
                            {
                                int r = rawData[rawIndex++];
                                int g = rawData[rawIndex++];
                                int b = rawData[rawIndex++];
                                
                                int rStretched = (int)((r - min) * scale);
                                int gStretched = (int)((g - min) * scale);
                                int bStretched = (int)((b - min) * scale);
                                
                                if (rStretched < 0) rStretched = 0; if (rStretched > 255) rStretched = 255;
                                if (gStretched < 0) gStretched = 0; if (gStretched > 255) gStretched = 255;
                                if (bStretched < 0) bStretched = 0; if (bStretched > 255) bStretched = 255;
                                
                                rgbValues[rgbIndex + x * 3] = (byte)bStretched;     // B
                                rgbValues[rgbIndex + x * 3 + 1] = (byte)gStretched; // G
                                rgbValues[rgbIndex + x * 3 + 2] = (byte)rStretched; // R
                            }
                        }
                    }
                }
                else if (bitsPerPixel == 16)
                {
                    if (channels == 1) // Monocromo 16-bit
                    {
                        ushort[] ushortData = new ushort[pixelCount];
                        for (int i = 0; i < pixelCount; i++)
                        {
                            ushortData[i] = (ushort)(rawData[i * 2] | (rawData[i * 2 + 1] << 8));
                            int val = ushortData[i];
                            if (val < min) min = val;
                            if (val > max) max = val;
                        }
                        
                        double scale = (max > min) ? 255.0 / (max - min) : 1.0;
                        int index = 0;
                        for (int y = 0; y < height; y++)
                        {
                            int rgbIndex = y * bmpData.Stride;
                            for (int x = 0; x < width; x++)
                            {
                                int rawVal = ushortData[index++];
                                int stretched = (int)((rawVal - min) * scale);
                                if (stretched < 0) stretched = 0;
                                if (stretched > 255) stretched = 255;
                                
                                rgbValues[rgbIndex + x * 3] = (byte)stretched;     // B
                                rgbValues[rgbIndex + x * 3 + 1] = (byte)stretched; // G
                                rgbValues[rgbIndex + x * 3 + 2] = (byte)stretched; // R
                            }
                        }
                    }
                    else if (channels == 3) // Color RGB 16-bit
                    {
                        ushort[] ushortData = new ushort[pixelCount * 3];
                        for (int i = 0; i < pixelCount * 3; i++)
                        {
                            ushortData[i] = (ushort)(rawData[i * 2] | (rawData[i * 2 + 1] << 8));
                            int val = ushortData[i];
                            if (val < min) min = val;
                            if (val > max) max = val;
                        }
                        
                        double scale = (max > min) ? 255.0 / (max - min) : 1.0;
                        int ushortIndex = 0;
                        for (int y = 0; y < height; y++)
                        {
                            int rgbIndex = y * bmpData.Stride;
                            for (int x = 0; x < width; x++)
                            {
                                int r = ushortData[ushortIndex++];
                                int g = ushortData[ushortIndex++];
                                int b = ushortData[ushortIndex++];
                                
                                int rStretched = (int)((r - min) * scale);
                                int gStretched = (int)((g - min) * scale);
                                int bStretched = (int)((b - min) * scale);
                                
                                if (rStretched < 0) rStretched = 0; if (rStretched > 255) rStretched = 255;
                                if (gStretched < 0) gStretched = 0; if (gStretched > 255) gStretched = 255;
                                if (bStretched < 0) bStretched = 0; if (bStretched > 255) bStretched = 255;
                                
                                rgbValues[rgbIndex + x * 3] = (byte)bStretched;     // B
                                rgbValues[rgbIndex + x * 3 + 1] = (byte)gStretched; // G
                                rgbValues[rgbIndex + x * 3 + 2] = (byte)rStretched; // R
                            }
                        }
                    }
                }
                
                Marshal.Copy(rgbValues, 0, ptr, bytes);
                bitmap.UnlockBits(bmpData);
                
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al convertir RAW a Bitmap: {0}", ex.Message);
                return null;
            }
        }

        unsafe private void btnStartCap_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Start Capture.***********************************************");

            // El modo Live no permite capturas, solo preview
            // El botón Start Capture debe estar deshabilitado en modo Live
            if (this.radioBtnLive.Checked == true)
            {
                Console.WriteLine("Start Capture no está disponible en modo Live (solo preview)");
                return;
            }

            quit = false;
            hasquit = false;

            // Solo modo Single puede usar Start Capture
            if (this.radioBtnSingle.Checked == true)
            {
                // Verificar carpeta de guardado para modo Single
                if (string.IsNullOrEmpty(saveFolderPath))
                {
                    MessageBox.Show("Por favor seleccione una carpeta de guardado antes de iniciar la captura.", 
                        "Carpeta no seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.btnStartCap.Enabled = true;
                    this.btnStopCap.Enabled = false;
                    // Los radio buttons permanecen habilitados en caso de error
                    return;
                }

                this.btnStartCap.Enabled = false;
                this.btnStopCap.Enabled = false;
                this.comBoxReadMode.Enabled = false;
                this.comBoxBinMode.Enabled = false;
                this.comBoxBits.Enabled = false;
                
                // Deshabilitar los radio buttons durante la captura
                this.radioBtnLive.Enabled = false;
                this.radioBtnSingle.Enabled = false;
                this.radioBtnRecording.Enabled = false;

                ASCOM.QHYCCD.libqhyccd.ExpQHYCCDSingleFrame(Common.camHandle);

                if (Common.canCooler)
                {
                    threadControlCooler.Suspend();
                }

                retVal = -1;
                while (retVal != 0)
                {
                    Application.DoEvents();
                    retVal = ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDSingleFrame(Common.camHandle, ref Common.camCurImgWidth, ref Common.camCurImgHeight, ref Common.camCurImgBits, ref Common.camCurImgChannels, rawArray);
                    Application.DoEvents();
                }

                if (retVal == 0)
                {
                    Console.WriteLine("Get Single Frame : w = {0} h = {1} bpp = {2} c = {3} date = {4}", Common.camCurImgWidth, Common.camCurImgHeight, Common.camCurImgBits, Common.camCurImgChannels, DateTime.Now);

                    if (this.btnGPSOnOff.Text == "OFF")
                    {
                        IntPtr buffer = Marshal.AllocHGlobal(44);
                        Marshal.Copy(rawArray, 0, buffer, 44);

                        var str = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), 44);
                        var data = new byte[44];
                        str.Read(data, 0, 44);
                        str.Position = 0;

                        SetTextCallBack deg = new SetTextCallBack(SetText);
                        this.Invoke(deg, new object[] { str });
                    }

                    bitmap = new Bitmap((int)Common.camCurImgWidth, (int)Common.camCurImgHeight);
                    rectangle = new Rectangle(0, 0, (int)Common.camCurImgWidth, (int)Common.camCurImgHeight);
                    bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    ptr = bmpData.Scan0;

                    s = 0;
                    index = 0;
                    pixData = 0;
                    rgbArray = new Byte[Common.camCurImgWidth * Common.camCurImgHeight * 4];
                    for (int i = 0; i < Common.camCurImgHeight; i++)
                    {
                        for (int y = 0; y < Common.camCurImgWidth; y++)
                        {
                            //Application.DoEvents();

                            if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 1)
                            {
                                rgbArray[s] = rawArray[index];
                                rgbArray[s + 1] = rawArray[index];
                                rgbArray[s + 2] = rawArray[index];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 1;
                            }
                            else if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 3)
                            {
                                rgbArray[s] = rawArray[index];
                                rgbArray[s + 1] = rawArray[index + 1];
                                rgbArray[s + 2] = rawArray[index + 2];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 3;
                            }
                            else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 1)
                            {
                                rgbArray[s]     = rawArray[index + 1];
                                rgbArray[s + 1] = rawArray[index + 1];
                                rgbArray[s + 2] = rawArray[index + 1];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 2;
                            }
                            else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 3)
                            {
                                rgbArray[s] = rawArray[index + 1];
                                rgbArray[s + 1] = rawArray[index + 3];
                                rgbArray[s + 2] = rawArray[index + 5];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 6;
                            }
                        }
                    }

                    Marshal.Copy(rgbArray, 0, ptr, (int)(Common.camCurImgWidth * Common.camCurImgHeight * 4));
                    bitmap.UnlockBits(bmpData);
                    pictureBox1.Image = bitmap;

                    // Guardar automáticamente en formato FITS
                    SaveImageToFITS(rawArray, Common.camCurImgWidth, Common.camCurImgHeight, 
                        Common.camCurImgBits, Common.camCurImgChannels, "SINGLE", 0);

                    GC.Collect();
                }

                hasquit = true;

                if (Common.canCooler)
                {
                    threadControlCooler.Resume();
                }

                this.btnStartCap.Enabled = true;
                this.btnStopCap.Enabled = false;
                this.comBoxReadMode.Enabled = true;
                this.comBoxBinMode.Enabled = true;
                this.comBoxBits.Enabled = false;
                
                // Habilitar los radio buttons después de completar la captura Single
                this.radioBtnLive.Enabled = true;
                this.radioBtnSingle.Enabled = true;
                this.radioBtnRecording.Enabled = true;
            }
        }

        void CameraStatus(object obj)
        {
            byte[] status = new byte[2];

            while (quitGetStatus != true)
            {
                retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDCameraStatus(Common.camHandle, status);

                if (status[0] == 0)
                    Console.WriteLine("Camera Status : IDLE");
                else if (status[0] == 1)
                    Console.WriteLine("Camera Status : EXPOSING");
                else
                    Console.WriteLine("Camera Status : READING");

                System.Threading.Thread.Sleep(200);
            }

            hasQuitGetStatus = true;
        }

        private uint ReadInt32(BinaryReader br)
        {
            uint p1 = br.ReadByte() * 65536U * 256U;
            uint p2 = br.ReadByte() * 65536U;
            uint p3 = br.ReadByte() * 256U;
            uint p4 = br.ReadByte();

            Console.WriteLine("ReadInt32 p1 = {0} p2 = {1} p3 = {2} p4 = {3}", p1, p2, p3, p4);
            return p1 + p2 + p3 + p4;
        }
        private short ReadInt16(BinaryReader br)
        {
            short p1 = (short)(br.ReadByte() * 256);
            short p2 = (short)br.ReadByte();

            Console.WriteLine("ReadInt16 p1 = {0} p2 = {1}", p1, p2);
            return (short)(p1 + p2);
        }

        private int ReadInt24(BinaryReader br)
        {
            int p1 = br.ReadByte() * 65536;
            int p2 = br.ReadByte() * 256;
            int p3 = br.ReadByte();

            Console.WriteLine("ReadInt24 p1 = {0} p2 = {1} p3 = {2}", p1, p2, p3);
            return p1 + p2 + p3;
        }
        private static double ParseLongitude(uint i)
        {
            //RAWLongitude = i;
            var west = i > 1000000000;
            var deg = (i % 1000000000) / 1000000;
            var min = (i % 1000000) / 10000;
            var fractMin = (i % 10000) / 10000.0;

            Console.WriteLine("ParseLongitude i = {0} west = {1} deg = {2} min = {3} fractMin = {4}", i, west, deg, min, fractMin);
            return (deg + (min + fractMin) / 60.0) * (west ? -1 : 1);
        }

        private static double ParseLatitude(uint i)
        {
            //RAWLatitude = i;
            var south = i > 1000000000;
            var deg = (i % 1000000000) / 10000000;
            var min = (i % 10000000) / 100000;
            var fractMin = (i % 100000) / 100000.0;

            Console.WriteLine("ParseLatitude i = {0} south = {1} deg = {2} min = {3} fractMin = {4}", i, south, deg, min, fractMin);
            return (deg + (min + fractMin) / 60.0) * (south ? -1 : 1);
        }
        static DateTime ParseTime(uint js)
        {
            var JD = js / 3600.0 / 24.0;
            Console.WriteLine("ParseTime js = {0} JD = {1}", js, JD);
            JD += +2450000.5;
            Console.WriteLine("ParseTime JD = {0}", JD);

            var result = DateTime.FromOADate(JD - 2415018.5);
            // now convert to a UTC time format *without* adjusting the actual time for the timezone offset - we then 
            // get a valid UTC date time as the result

            Console.WriteLine("ParseTime Year = {0} Month = {1} Day = {2} Hour = {3} Minute = {4} Second = {5}", result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second);
            return new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, DateTimeKind.Utc);
        }
        internal StatusFlag Status { get; private set; }

        internal enum StatusFlag
        {
            NoGPS,
            Initializing,
            PartialData,
            Locked,
            BadTimestamp,
        }

        delegate void SetTextCallBack(UnmanagedMemoryStream str);
        delegate void UpdatePictureBoxCallBack(Bitmap bmp);
        
        private void UpdatePictureBox(Bitmap bmp)
        {
            if (this.pictureBox1 != null && bmp != null)
            {
                // Liberar la imagen anterior si existe
                if (this.pictureBox1.Image != null)
                {
                    var oldImage = this.pictureBox1.Image;
                    this.pictureBox1.Image = null;
                    oldImage.Dispose();
                }
                this.pictureBox1.Image = bmp;
            }
        }
        
        private void SetText(UnmanagedMemoryStream str)
        {
            var br = new BinaryReader(str);
            uint SequenceNum = ReadInt32(br);
            Console.WriteLine("SetText SequenceNum = {0}", SequenceNum);

            uint TempSequenceNum = br.ReadByte();
            Console.WriteLine("SetText TempSequenceNum = {0}", TempSequenceNum);

            short width = ReadInt16(br);
            Console.WriteLine("SetText width = {0}", width);

            short height = ReadInt16(br);
            Console.WriteLine("SetText height = {0}", height);

            double Latitude = ParseLatitude(ReadInt32(br));
            Console.WriteLine("SetText height = {0}", height);

            double Longitude = ParseLongitude(ReadInt32(br));
            Console.WriteLine("SetText longitude = {0}", Longitude);

            uint StartFlag = br.ReadByte();
            Console.WriteLine("SetText StartFlag = {0}", StartFlag);

            DateTime StartShutterTime = ParseTime(ReadInt32(br));
            Console.WriteLine("SetText StartShutterTime = {0}", StartShutterTime);

            double StartShutterMicroSeconds = ReadInt24(br) / 10.0;
            Console.WriteLine("SetText StartShutterMicroSeconds = {0}", StartShutterMicroSeconds);

            uint EndFlag = br.ReadByte();
            Console.WriteLine("SetText EndFlag = {0}", EndFlag);

            DateTime EndShutterTime = ParseTime(ReadInt32(br));
            Console.WriteLine("SetText EndShutterTime = {0}", EndShutterTime);

            double EndShutterMicroSeconds = ReadInt24(br) / 10.0;
            Console.WriteLine("SetText EndShutterMicroSeconds = {0}", EndShutterMicroSeconds);

            uint NowFlag = br.ReadByte();
            Console.WriteLine("SetText NowFlag = {0}", NowFlag);

            DateTime NowShutterTime = ParseTime(ReadInt32(br));
            Console.WriteLine("SetText NowShutterTime = {0}", NowShutterTime);

            double NowShutterMicroSeconds = ReadInt24(br) / 10.0;
            Console.WriteLine("SetText NowShutterMicroSeconds = {0}", NowShutterMicroSeconds);

            double PPSCounter = ReadInt24(br);
            Console.WriteLine("SetText PPSCounter = {0}", PPSCounter);

            StatusFlag Status = (StatusFlag)((NowFlag / 16) % 4);
            Console.WriteLine("SetText Status = {0}", Status);

            Console.WriteLine("Now Time = {0} {1}", DateTime.UtcNow, Math.Abs((StartShutterTime - DateTime.UtcNow).TotalSeconds));
            if (Math.Abs((StartShutterTime - DateTime.UtcNow).TotalSeconds) > 86400)
            {
                Status = StatusFlag.BadTimestamp;
            }
            if (Status == StatusFlag.Locked)
            {
            }

            Console.WriteLine("Expose Time = {0} {1}", (EndShutterTime - StartShutterTime).TotalMilliseconds * 1000, (EndShutterMicroSeconds - StartShutterMicroSeconds));
            var exposure = (EndShutterTime - StartShutterTime).TotalMilliseconds * 1000 + (EndShutterMicroSeconds - StartShutterMicroSeconds);

            this.statusvalue.Text = Status.ToString();
            this.sequencevalue.Text = SequenceNum.ToString();
            this.latitudevalue.Text = Latitude.ToString();
            this.longitudevalue.Text = Longitude.ToString();
            this.startvalue.Text = StartShutterTime.ToString();
            this.startusvalue.Text = StartShutterMicroSeconds.ToString();
            this.endvalue.Text = EndShutterTime.ToString();
            this.endusvalue.Text = EndShutterMicroSeconds.ToString();
            this.nowvalue.Text = NowShutterTime.ToString();
            this.nowusvalue.Text = NowShutterMicroSeconds.ToString();
            this.ppsvalue.Text = PPSCounter.ToString();
            this.expvalue.Text = exposure.ToString();
        }

        unsafe void ShowLiveImage(object obj)
        {
            Console.WriteLine("ShowLiveImage thread iniciado");
            int frameAttempts = 0;
            int lastError = 0;
            
            while (quit != true)
            {
                 //Console.WriteLine("START : {0} {1} {2}", DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond);
                retVal = -1;
                frameAttempts = 0;
                while (retVal != 0 && quit != true)
                {
                    retVal = ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDLiveFrame(Common.camHandle, ref Common.camCurImgWidth, ref Common.camCurImgHeight, ref Common.camCurImgBits, ref Common.camCurImgChannels, rawArray);
                    frameAttempts++;
                    
                    if (retVal != 0 && frameAttempts % 100 == 0)
                    {
                        lastError = retVal;
                        Console.WriteLine("GetQHYCCDLiveFrame intento {0}, retVal={1}, w={2}, h={3}, bpp={4}, ch={5}", 
                            frameAttempts, retVal, Common.camCurImgWidth, Common.camCurImgHeight, Common.camCurImgBits, Common.camCurImgChannels);
                    }
                    
                    if (retVal != 0)
                    {
                        System.Threading.Thread.Sleep(10); // Pequeño delay entre intentos
                    }
                    
                    Application.DoEvents();
                }

                if (retVal == 0)
                {
                    Console.WriteLine("Live Frame obtenido: w={0} h={1} bpp={2} ch={3}", 
                        Common.camCurImgWidth, Common.camCurImgHeight, Common.camCurImgBits, Common.camCurImgChannels);
                    
                    // Validar que las dimensiones sean válidas
                    if (Common.camCurImgWidth == 0 || Common.camCurImgHeight == 0 || 
                        Common.camCurImgWidth > Common.camMaxImageWidth || 
                        Common.camCurImgHeight > Common.camMaxImageHeight)
                    {
                        Console.WriteLine("Dimensiones inválidas del frame, saltando...");
                        continue;
                    }
                    
                    if(Common.burstOnOff && Common.burstCap)
                    {
                        Common.burstCapNum++;
                        Console.WriteLine("Burst Capture : {0}", Common.burstCapNum);
                        
                        if (Common.burstCapNum >= Common.burstCapTarget)
                            Common.burstCap = false;
                    }

                    // Calcular offset para GPS si está activo
                    int gpsOffset = 0;
                    if (this.btnGPSOnOff != null && this.btnGPSOnOff.Text == "OFF")
                    {
                        gpsOffset = 44; // Los primeros 44 bytes son metadata GPS
                        
                        IntPtr buffer = Marshal.AllocHGlobal(44);
                        Marshal.Copy(rawArray, 0, buffer, 44);

                        var str = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), 44);
                        var data = new byte[44];
                        str.Read(data, 0, 44);
                        str.Position = 0;

                        SetTextCallBack deg = new SetTextCallBack(SetText);
                        this.Invoke(deg, new object[] { str });
                    }

                    // Crear bitmap con formato ARGB32 explícito para mejor compatibilidad
                    bitmap = new Bitmap((int)Common.camCurImgWidth, (int)Common.camCurImgHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    rectangle = new Rectangle(0, 0, (int)Common.camCurImgWidth, (int)Common.camCurImgHeight);
                    bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    ptr = bmpData.Scan0;

                    s = 0;
                    index = gpsOffset; // Iniciar desde el offset GPS si está activo
                    pixData = 0;
                    rgbArray = new Byte[Common.camCurImgWidth * Common.camCurImgHeight * 4];
                    for (int i = 0; i < Common.camCurImgHeight; i++)
                    {
                        for (int y = 0; y < Common.camCurImgWidth; y++)
                        {
                            if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 1)
                            {
                                rgbArray[s] = rawArray[index];
                                rgbArray[s + 1] = rawArray[index];
                                rgbArray[s + 2] = rawArray[index];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 1;
                            }
                            else if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 3)
                            {
                                rgbArray[s] = rawArray[index];
                                rgbArray[s + 1] = rawArray[index + 1];
                                rgbArray[s + 2] = rawArray[index + 2];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 3;
                            }
                            else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 1)
                            {
                                rgbArray[s] = rawArray[index + 1];
                                rgbArray[s + 1] = rawArray[index + 1];
                                rgbArray[s + 2] = rawArray[index + 1];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 2;
                            }
                            else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 3)
                            {
                                rgbArray[s] = rawArray[index + 1];
                                rgbArray[s + 1] = rawArray[index + 3];
                                rgbArray[s + 2] = rawArray[index + 5];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 6;
                            }
                        }
                    }

                    Marshal.Copy(rgbArray, 0, ptr, (int)(Common.camCurImgWidth * Common.camCurImgHeight * 4));
                    bitmap.UnlockBits(bmpData);
                    
                    // Actualizar pictureBox desde el thread principal usando Invoke
                    if (this.InvokeRequired)
                    {
                        UpdatePictureBoxCallBack updateCallback = new UpdatePictureBoxCallBack(UpdatePictureBox);
                        this.Invoke(updateCallback, new object[] { bitmap });
                        Console.WriteLine("PictureBox actualizado desde thread secundario");
                    }
                    else
                    {
                        UpdatePictureBox(bitmap);
                        Console.WriteLine("PictureBox actualizado desde thread principal");
                    }

                    // Modo Live: Solo preview, no se guardan archivos
                    // (El guardado de archivos se ha eliminado para modo preview)

                    GC.Collect();
                    
                    // Pequeño delay para no saturar el sistema y permitir que la UI se actualice
                    System.Threading.Thread.Sleep(50);
                }
                else if (quit == false && retVal != 0)
                {
                    // Si no se obtuvo frame pero no se debe salir, esperar un poco más
                    System.Threading.Thread.Sleep(100);
                }
            }

            hasquit = true;
            Console.WriteLine("Thread Live Has Quit.");
        }

        unsafe void ShowSingleImage(object obj)
        {
            while (quit != true)
            {
                ASCOM.QHYCCD.libqhyccd.ExpQHYCCDSingleFrame(Common.camHandle);

                retVal = -1;
                while (retVal != 0)
                {
                    retVal = ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDSingleFrame(Common.camHandle, ref Common.camCurImgWidth, ref Common.camCurImgHeight, ref Common.camCurImgBits, ref Common.camCurImgChannels, rawArray);
                    Application.DoEvents();
                }

                if (retVal == 0)
                {
                    Console.WriteLine("Single w = {0} h = {1} bpp = {2} c = {3}", Common.camCurImgWidth, Common.camCurImgHeight, Common.camCurImgBits, Common.camCurImgChannels);
                    if (this.btnGPSOnOff.Text == "OFF")
                    {
                        IntPtr buffer = Marshal.AllocHGlobal(44);
                        Marshal.Copy(rawArray, 0, buffer, 44);

                        var str = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), 44);
                        var data = new byte[44];
                        str.Read(data, 0, 44);
                        str.Position = 0;

                        SetTextCallBack deg = new SetTextCallBack(SetText);
                        this.Invoke(deg, new object[] { str });
                    }

                    bitmap = new Bitmap((int)Common.camCurImgWidth, (int)Common.camCurImgHeight);
                    rectangle = new Rectangle(0, 0, (int)Common.camCurImgWidth, (int)Common.camCurImgHeight);
                    bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    ptr = bmpData.Scan0;

                    s = 0;
                    index = 0;
                    pixData = 0;
                    rgbArray = new Byte[Common.camCurImgWidth * Common.camCurImgHeight * 4];
                    for (int i = 0; i < Common.camCurImgHeight; i++)
                    {
                        for (int y = 0; y < Common.camCurImgWidth; y++)
                        {
                            if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 1)
                            {
                                rgbArray[s] = rawArray[index];
                                rgbArray[s + 1] = rawArray[index];
                                rgbArray[s + 2] = rawArray[index];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 1;
                            }
                            else if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 3)
                            {
                                rgbArray[s] = rawArray[index];
                                rgbArray[s + 1] = rawArray[index + 1];
                                rgbArray[s + 2] = rawArray[index + 2];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 3;
                            }
                            else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 1)
                            {
                                rgbArray[s] = rawArray[index + 1];
                                rgbArray[s + 1] = rawArray[index + 1];
                                rgbArray[s + 2] = rawArray[index + 1];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 2;
                            }
                            else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 3)
                            {
                                rgbArray[s] = rawArray[index + 1];
                                rgbArray[s + 1] = rawArray[index + 3];
                                rgbArray[s + 2] = rawArray[index + 5];
                                rgbArray[s + 3] = 255;

                                s += 4;
                                index += 6;
                            }
                        }
                    }

                    Marshal.Copy(rgbArray, 0, ptr, (int)(Common.camCurImgWidth * Common.camCurImgHeight * 4));
                    bitmap.UnlockBits(bmpData);
                    pictureBox1.Image = bitmap;

                    //bitmap.Clone
                    //bitmap.Save("D:\\save\\save.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                    GC.Collect();
                }
            }

            hasquit = true;
            Console.WriteLine("Thread Single Has Quit.");
        }

        /****************************************************************************/
        /************************** Recording Mode Functions ***********************/
        /****************************************************************************/

        private void StartRecording()
        {
            if (recordingConfig == null || isRecording)
                return;

            // Inicializar contadores
            recordingFrameCounter = 0;
            recordingSequenceCounter = 0;
            recordingStartTime = DateTime.UtcNow;
            isRecording = true;
            isRecordingPaused = false;

            // Mostrar controles de grabación
            // Nota: No deshabilitamos los radio buttons para permitir cambio de modo
            // pero sí deshabilitamos Start Capture ya que Recording maneja sus propias capturas
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    progressBarRecording.Visible = true;
                    labelRecordingProgress.Visible = true;
                    btnPauseResume.Visible = true;
                    btnPauseResume.Text = "Pausa";
                    this.btnStartCap.Enabled = false; // Deshabilitar Start Capture durante Recording
                    this.btnStopCap.Enabled = true; // Habilitar Stop para cancelar Recording
                    // Los radio buttons permanecen habilitados para permitir cambio de modo
                }));
            }
            else
            {
                progressBarRecording.Visible = true;
                labelRecordingProgress.Visible = true;
                btnPauseResume.Visible = true;
                btnPauseResume.Text = "Pausa";
                this.btnStartCap.Enabled = false;
                this.btnStopCap.Enabled = true;
            }

            // Iniciar thread de grabación
            quit = false;
            hasquit = false;
            threadRecording = new Thread(new ParameterizedThreadStart(RecordingThread));
            threadRecording.Start();
            
            Console.WriteLine("Grabación iniciada");
        }

        private void BtnPauseResume_Click(object sender, EventArgs e)
        {
            if (!isRecording)
                return;

            isRecordingPaused = !isRecordingPaused;
            
            if (isRecordingPaused)
            {
                btnPauseResume.Text = "Reanudar";
                Console.WriteLine("Grabación pausada");
            }
            else
            {
                btnPauseResume.Text = "Pausa";
                Console.WriteLine("Grabación reanudada");
            }
        }

        unsafe void RecordingThread(object obj)
        {
            Console.WriteLine("Recording thread iniciado");
            
            uint totalFramesInSession = 0;
            uint currentSequence = 0;
            DateTime sessionStartTime = DateTime.MinValue;
            TimeSpan remainingTime = TimeSpan.Zero;

            while (quit != true && isRecording)
            {
                // Manejar secuencia simple
                if (recordingConfig.UseSequence)
                {
                    if (currentSequence == 0)
                    {
                        // Iniciar nueva secuencia
                        currentSequence = 1;
                        sessionStartTime = DateTime.UtcNow;
                        recordingFrameCounter = 0;
                        totalFramesInSession = 0;
                        Console.WriteLine("Iniciando secuencia {0}/{1}", currentSequence, recordingConfig.SequenceLength);
                    }
                    else if (currentSequence < recordingConfig.SequenceLength)
                    {
                        // Verificar si debemos esperar intervalo entre secuencias
                        // (esto se maneja después de completar una sesión)
                    }
                }
                else
                {
                    // Sin secuencia, solo una sesión
                    if (sessionStartTime == DateTime.MinValue)
                    {
                        sessionStartTime = DateTime.UtcNow;
                        recordingFrameCounter = 0;
                    }
                }

                // Verificar límites de la sesión actual
                bool sessionComplete = false;
                
                if (recordingConfig.LimitType == RecordingLimitType.FrameCount)
                {
                    if (recordingFrameCounter >= recordingConfig.FrameCount)
                    {
                        sessionComplete = true;
                    }
                }
                else if (recordingConfig.LimitType == RecordingLimitType.TimeLimit)
                {
                    TimeSpan elapsed = DateTime.UtcNow - sessionStartTime;
                    remainingTime = recordingConfig.TimeLimit - elapsed;
                    if (remainingTime <= TimeSpan.Zero)
                    {
                        sessionComplete = true;
                    }
                }
                // Unlimited: nunca se completa automáticamente

                if (sessionComplete && !recordingConfig.UseSequence)
                {
                    // Sesión completa y no hay secuencia, terminar
                    break;
                }
                else if (sessionComplete && recordingConfig.UseSequence)
                {
                    // Sesión completa, avanzar a siguiente secuencia
                    currentSequence++;
                    if (currentSequence > recordingConfig.SequenceLength)
                    {
                        // Todas las secuencias completadas
                        break;
                    }
                    
                    // Esperar intervalo entre secuencias
                    Console.WriteLine("Esperando intervalo de {0} antes de iniciar secuencia {1}/{2}", 
                        recordingConfig.SequenceInterval, currentSequence, recordingConfig.SequenceLength);
                    
                    DateTime intervalStart = DateTime.UtcNow;
                    while ((DateTime.UtcNow - intervalStart) < recordingConfig.SequenceInterval && quit != true && isRecording)
                    {
                        if (!isRecordingPaused)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        Application.DoEvents();
                    }
                    
                    // Iniciar nueva secuencia
                    sessionStartTime = DateTime.UtcNow;
                    recordingFrameCounter = 0;
                    totalFramesInSession = 0;
                    Console.WriteLine("Iniciando secuencia {0}/{1}", currentSequence, recordingConfig.SequenceLength);
                }

                // Esperar si está pausado
                while (isRecordingPaused && quit != true && isRecording)
                {
                    System.Threading.Thread.Sleep(100);
                    Application.DoEvents();
                }

                if (quit || !isRecording)
                    break;

                // Tomar captura
                ASCOM.QHYCCD.libqhyccd.ExpQHYCCDSingleFrame(Common.camHandle);

                retVal = -1;
                while (retVal != 0 && quit != true && isRecording)
                {
                    retVal = ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDSingleFrame(Common.camHandle, ref Common.camCurImgWidth, ref Common.camCurImgHeight, ref Common.camCurImgBits, ref Common.camCurImgChannels, rawArray);
                    Application.DoEvents();
                }

                if (retVal == 0 && !quit && isRecording)
                {
                    recordingFrameCounter++;
                    totalFramesInSession++;
                    
                    // Mostrar preview solo si NO está en modo Live (Live mode ya muestra el preview automáticamente)
                    // Si está en modo Live, el preview se muestra desde ShowLiveImage thread
                    if (!this.radioBtnLive.Checked)
                    {
                        // Procesar y mostrar imagen cuando no está en modo Live
                        ProcessAndDisplayImage();
                    }
                    // Si está en modo Live, el preview ya se está mostrando desde ShowLiveImage

                    // Guardar archivo FITS
                    DateTime now = DateTime.UtcNow;
                    string fileName = string.Format("{0}_{1:D4}.fit", 
                        recordingConfig.DestinationName, recordingFrameCounter);
                    string filePath = Path.Combine(recordingSessionFolder, fileName);
                    
                    SaveImageToFITS(rawArray, Common.camCurImgWidth, Common.camCurImgHeight, 
                        Common.camCurImgBits, Common.camCurImgChannels, "RECORDING", recordingFrameCounter, filePath);

                    // Actualizar progreso
                    UpdateRecordingProgress(recordingConfig, recordingFrameCounter, totalFramesInSession, 
                        currentSequence, sessionStartTime, remainingTime);
                }

                Application.DoEvents();
            }

            // Finalizar grabación
            StopRecording();
            hasquit = true;
            Console.WriteLine("Recording thread finalizado");
        }

        unsafe private void ProcessAndDisplayImage()
        {
            // Procesar y mostrar imagen en pictureBox (similar a ShowSingleImage)
            try
            {
                // Calcular offset para GPS si está activo
                int gpsOffset = 0;
                if (this.btnGPSOnOff != null && this.btnGPSOnOff.Text == "OFF")
                {
                    gpsOffset = 44;
                }

                // Crear bitmap
                Bitmap displayBitmap = new Bitmap((int)Common.camCurImgWidth, (int)Common.camCurImgHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Rectangle displayRectangle = new Rectangle(0, 0, (int)Common.camCurImgWidth, (int)Common.camCurImgHeight);
                BitmapData displayBmpData = displayBitmap.LockBits(displayRectangle, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                IntPtr displayPtr = displayBmpData.Scan0;

                int s = 0;
                int index = gpsOffset;
                Byte[] displayRgbArray = new Byte[Common.camCurImgWidth * Common.camCurImgHeight * 4];
                
                for (int i = 0; i < Common.camCurImgHeight; i++)
                {
                    for (int y = 0; y < Common.camCurImgWidth; y++)
                    {
                        if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 1)
                        {
                            displayRgbArray[s] = rawArray[index];
                            displayRgbArray[s + 1] = rawArray[index];
                            displayRgbArray[s + 2] = rawArray[index];
                            displayRgbArray[s + 3] = 255;
                            s += 4;
                            index += 1;
                        }
                        else if (Common.camCurImgBits == 8 && Common.camCurImgChannels == 3)
                        {
                            displayRgbArray[s] = rawArray[index];
                            displayRgbArray[s + 1] = rawArray[index + 1];
                            displayRgbArray[s + 2] = rawArray[index + 2];
                            displayRgbArray[s + 3] = 255;
                            s += 4;
                            index += 3;
                        }
                        else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 1)
                        {
                            displayRgbArray[s] = rawArray[index + 1];
                            displayRgbArray[s + 1] = rawArray[index + 1];
                            displayRgbArray[s + 2] = rawArray[index + 1];
                            displayRgbArray[s + 3] = 255;
                            s += 4;
                            index += 2;
                        }
                        else if (Common.camCurImgBits == 16 && Common.camCurImgChannels == 3)
                        {
                            displayRgbArray[s] = rawArray[index + 1];
                            displayRgbArray[s + 1] = rawArray[index + 3];
                            displayRgbArray[s + 2] = rawArray[index + 5];
                            displayRgbArray[s + 3] = 255;
                            s += 4;
                            index += 6;
                        }
                    }
                }

                Marshal.Copy(displayRgbArray, 0, displayPtr, (int)(Common.camCurImgWidth * Common.camCurImgHeight * 4));
                displayBitmap.UnlockBits(displayBmpData);

                // Actualizar pictureBox desde el thread principal usando Invoke
                if (this.InvokeRequired)
                {
                    UpdatePictureBoxCallBack updateCallback = new UpdatePictureBoxCallBack(UpdatePictureBox);
                    this.Invoke(updateCallback, new object[] { displayBitmap });
                }
                else
                {
                    UpdatePictureBox(displayBitmap);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al procesar imagen para display: {0}", ex.Message);
            }
        }

        private void UpdateRecordingProgress(RecordingConfigForm config, uint currentFrame, uint totalFrames, 
            uint currentSequence, DateTime sessionStart, TimeSpan remaining)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateRecordingProgressUI(config, currentFrame, totalFrames, 
                    currentSequence, sessionStart, remaining)));
            }
            else
            {
                UpdateRecordingProgressUI(config, currentFrame, totalFrames, currentSequence, sessionStart, remaining);
            }
        }

        private void UpdateRecordingProgressUI(RecordingConfigForm config, uint currentFrame, uint totalFrames, 
            uint currentSequence, DateTime sessionStart, TimeSpan remaining)
        {
            string progressText = "";
            int progressValue = 0;

            if (config.LimitType == RecordingLimitType.Unlimited)
            {
                progressText = string.Format("Frames: {0}", currentFrame);
                progressValue = 0; // Sin límite, no mostrar porcentaje
            }
            else if (config.LimitType == RecordingLimitType.FrameCount)
            {
                progressValue = (int)((currentFrame * 100) / config.FrameCount);
                if (config.UseSequence)
                {
                    progressText = string.Format("Secuencia {0}/{1} - Frame {2}/{3}", 
                        currentSequence, config.SequenceLength, currentFrame, config.FrameCount);
                }
                else
                {
                    progressText = string.Format("Frame {0}/{1}", currentFrame, config.FrameCount);
                }
            }
            else if (config.LimitType == RecordingLimitType.TimeLimit)
            {
                if (remaining > TimeSpan.Zero)
                {
                    progressText = string.Format("Tiempo restante: {0:D2}:{1:D2}:{2:D2}", 
                        remaining.Hours, remaining.Minutes, remaining.Seconds);
                }
                else
                {
                    progressText = "Tiempo agotado";
                }
                
                if (config.TimeLimit.TotalSeconds > 0)
                {
                    TimeSpan elapsed = DateTime.UtcNow - sessionStart;
                    progressValue = (int)((elapsed.TotalSeconds * 100) / config.TimeLimit.TotalSeconds);
                    if (progressValue > 100) progressValue = 100;
                }
                
                if (config.UseSequence)
                {
                    progressText += string.Format(" - Secuencia {0}/{1}", currentSequence, config.SequenceLength);
                }
            }

            labelRecordingProgress.Text = progressText;
            progressBarRecording.Value = progressValue;
        }

        private void StopRecording()
        {
            isRecording = false;
            isRecordingPaused = false;
            quit = true;

            // Esperar a que el thread termine
            if (threadRecording != null && threadRecording.IsAlive)
            {
                while (hasquit != true)
                {
                    Application.DoEvents();
                }
                threadRecording.Abort();
            }

            // Ocultar controles de grabación
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    progressBarRecording.Visible = false;
                    labelRecordingProgress.Visible = false;
                    btnPauseResume.Visible = false;
                    // Restaurar estado de botones según el modo actual
                    if (this.radioBtnSingle.Checked)
                    {
                        this.btnStartCap.Enabled = true;
                        this.btnStopCap.Enabled = false;
                    }
                    else if (this.radioBtnLive.Checked)
                    {
                        this.btnStartCap.Enabled = false;
                        this.btnStopCap.Enabled = false;
                    }
                    else
                    {
                        this.btnStartCap.Enabled = true;
                        this.btnStopCap.Enabled = false;
                    }
                }));
            }
            else
            {
                progressBarRecording.Visible = false;
                labelRecordingProgress.Visible = false;
                btnPauseResume.Visible = false;
                // Restaurar estado de botones según el modo actual
                if (this.radioBtnSingle.Checked)
                {
                    this.btnStartCap.Enabled = true;
                    this.btnStopCap.Enabled = false;
                }
                else if (this.radioBtnLive.Checked)
                {
                    this.btnStartCap.Enabled = false;
                    this.btnStopCap.Enabled = false;
                }
                else
                {
                    this.btnStartCap.Enabled = true;
                    this.btnStopCap.Enabled = false;
                }
            }

            Console.WriteLine("Grabación detenida");
        }

        private void btnStopCap_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Stop Capture.***********************************************");

            // Si está en modo Recording, detener grabación
            if (this.radioBtnRecording.Checked && isRecording)
            {
                StopRecording();
                return;
            }

            this.DisConnection.Enabled = false;
            this.btnStopCap.Enabled = false;

            int ret = -1;
            quit = true;

            while(hasquit != true)
            {
                Application.DoEvents();
            }

            this.btnStartCap.Enabled = true;
            this.DisConnection.Enabled = true;

            if (Common.canLive == true)
            {
                if (threadShowLiveImage != null && threadShowLiveImage.IsAlive)
                    threadShowLiveImage.Abort();
            }
            else
            {
                if (threadShowSingleImage != null && threadShowSingleImage.IsAlive)
                    threadShowSingleImage.Abort();
            }

            if (Common.canLive == true)
            {
                ret = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);
            }
            else
            {
                ret = ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);
            }

            if (Common.canLive && this.radioBtnLive.Checked)
            {
                if (Common.burstOnOff)
                {
                    Common.burstOnOff = false;
                    Common.burstCap   = false;
                    this.btnBurstOnOff.PerformClick();

                    this.btnBurstOnOff.Enabled = true;
                    this.btnBurstStartEnd.Enabled = false;
                    this.textBoxBurstStart.Enabled = false;
                    this.textBoxBurstEnd.Enabled = false;
                    this.btnBurstPatch.Enabled = false;
                    this.textBoxBurstPatch.Enabled = false;
                    this.btnBurstCapture.Enabled = false;
                }
            }

            // Habilitar los radio buttons después de detener la captura
            this.radioBtnLive.Enabled = true;
            this.radioBtnSingle.Enabled = true;
            this.radioBtnRecording.Enabled = true;
        }

        /****************************************************************************/
        /******************** Helper Methods for Mode Switching ********************/
        /****************************************************************************/

        /// <summary>
        /// Obtiene el último número de frame usado en archivos Single existentes
        /// </summary>
        /// <param name="folderPath">Ruta de la carpeta donde se guardan los archivos</param>
        /// <returns>El último número de frame encontrado, o 0 si no hay archivos</returns>
        private uint GetLastSingleFrameNumber(string folderPath)
        {
            return GetLastSingleFrameNumber(folderPath, Common.imageFileFormat);
        }

        private uint GetLastSingleFrameNumber(string folderPath, string format)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return 0;
            }

            try
            {
                // Buscar archivos según el formato seleccionado
                string extension = format == "PNG" ? "*.png" : "*.fit";
                string[] files = Directory.GetFiles(folderPath, "Single_*" + extension.Replace("*", ""));
                uint maxNumber = 0;

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    // El formato es: Single_YYYYMMDD_HHMMSS_XXXX.fit
                    // Necesitamos extraer el número después del último guion bajo
                    int lastUnderscore = fileName.LastIndexOf('_');
                    int dotIndex = fileName.LastIndexOf('.');
                    
                    if (lastUnderscore > 0 && dotIndex > lastUnderscore)
                    {
                        string numberStr = fileName.Substring(lastUnderscore + 1, dotIndex - lastUnderscore - 1);
                        if (uint.TryParse(numberStr, out uint number))
                        {
                            if (number > maxNumber)
                            {
                                maxNumber = number;
                            }
                        }
                    }
                }

                return maxNumber;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al buscar último número de frame: {0}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Verifica si la cámara está actualmente capturando imágenes
        /// </summary>
        /// <returns>true si está capturando, false en caso contrario</returns>
        private bool IsCapturing()
        {
            // En modo Live no hay captura (solo preview), así que no se considera "capturando"
            // Solo modo Single puede estar capturando
            if (this.radioBtnLive.Checked && Common.canLive)
            {
                return false; // Modo Live no está "capturando", solo haciendo preview
            }
            
            // Si está grabando en modo Recording
            if (this.radioBtnRecording.Checked && isRecording)
            {
                return true;
            }
            
            // Si btnStopCap está habilitado, significa que está capturando (solo en modo Single)
            return this.btnStopCap.Enabled == true;
        }

        /// <summary>
        /// Cambia el modo de captura (Single/Live) mientras la cámara está conectada
        /// </summary>
        /// <param name="newMode">0 para Single, 1 para Live</param>
        private void ChangeCaptureMode(uint newMode)
        {
            if (!isConnect)
            {
                return; // La cámara no está conectada
            }

            Console.WriteLine("Cambiando modo de captura a: {0}", newMode == 0 ? "Single" : "Live");

            int retVal = -1;
            int ret = -1;

            // Detener cualquier captura activa si existe
            bool wasCapturing = false;
            if (threadShowLiveImage != null && threadShowLiveImage.IsAlive)
            {
                wasCapturing = true;
                quit = true;
                while (hasquit != true)
                {
                    Application.DoEvents();
                }
                threadShowLiveImage.Abort();
                if (Common.canLive == true)
                {
                    ret = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);
                }
            }

            if (threadShowSingleImage != null && threadShowSingleImage.IsAlive)
            {
                wasCapturing = true;
                quit = true;
                while (hasquit != true)
                {
                    Application.DoEvents();
                }
                threadShowSingleImage.Abort();
                ret = ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);
            }
            
            // Si estaba capturando, habilitar los botones de control
            if (wasCapturing)
            {
                this.btnStartCap.Enabled = true;
                this.btnStopCap.Enabled = false;
            }

            // Actualizar el modo de stream
            Common.camStreamMode = newMode;
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
            
            if (retVal != 0)
            {
                MessageBox.Show("Error al cambiar el modo de captura. Por favor, intente nuevamente.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Reinicializar la cámara
            retVal = ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);
            if (retVal != 0)
            {
                MessageBox.Show("Error al reinicializar la cámara después del cambio de modo.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Restablecer parámetros de la cámara
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
            
            if (this.checkBoxOS.Checked)
            {
                ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                Common.camImageStartX = Common.camEFStartX;
                Common.camImageStartY = Common.camEFStartY;
                Common.camImageSizeX = Common.camEFSizeX;
                Common.camImageSizeY = Common.camEFSizeY / 2 * 2;
            }
            else
            {
                Common.camImageStartX = 0;
                Common.camImageStartY = 0;
                Common.camImageSizeX = Common.camMinImageWidth / Common.camBinX;
                Common.camImageSizeY = Common.camMinImageHeight / Common.camBinY / 2 * 2;
            }
            
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);

            quit = false;
            hasquit = false;

            // Si se cambió a modo Live, iniciar automáticamente el preview
            if (newMode == 1 && Common.canLive)
            {
                // Verificar que rawArray tenga el tamaño correcto (usar tamaño máximo)
                if (rawArray == null || rawArray.Length < (Common.camMaxImageWidth * Common.camMaxImageHeight * 4))
                {
                    Console.WriteLine("Redimensionando rawArray en ChangeCaptureMode: tamaño actual={0}, tamaño máximo={1}", 
                        rawArray != null ? rawArray.Length : 0, Common.camMaxImageWidth * Common.camMaxImageHeight * 4);
                    rawArray = new byte[Common.camMaxImageWidth * Common.camMaxImageHeight * 4];
                }
                
                // Iniciar modo Live para preview
                uint beginResult = ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(Common.camHandle);
                Console.WriteLine("BeginQHYCCDLive en ChangeCaptureMode retornó: {0}", beginResult);
                
                if (beginResult == 0)
                {
                    // Pequeño delay para que la cámara inicie el modo Live
                    System.Threading.Thread.Sleep(100);
                    
                    // Iniciar thread para mostrar el preview
                    threadShowLiveImage = new Thread(new ParameterizedThreadStart(ShowLiveImage));
                    threadShowLiveImage.Start();
                    
                    // Deshabilitar botón Start Capture en modo Live (solo preview, no captura)
                    this.btnStartCap.Enabled = false;
                    this.btnStopCap.Enabled = false;
                    
                    Console.WriteLine("Modo Live iniciado automáticamente para preview");
                }
                else
                {
                    Console.WriteLine("Error al iniciar modo Live en ChangeCaptureMode: BeginQHYCCDLive retornó {0}", beginResult);
                }
            }
            else if (newMode == 0)
            {
                // Si se cambió a modo Single, habilitar botón Start Capture
                this.btnStartCap.Enabled = true;
                this.btnStopCap.Enabled = false;
            }

            Console.WriteLine("Modo de captura cambiado exitosamente a: {0}", newMode == 0 ? "Single" : "Live");
        }

        /// <summary>
        /// Event handler para cuando se cambia el radio button Single
        /// </summary>
        private void radioBtnSingle_CheckedChanged(object sender, EventArgs e)
        {
            // Evitar recursión si ya estamos cambiando el modo
            if (isChangingMode)
            {
                return;
            }

            // Solo procesar si el radio button está marcado y la cámara está conectada
            if (!this.radioBtnSingle.Checked || !isConnect)
            {
                return;
            }

            // Si está grabando, detener grabación antes de cambiar de modo
            if (isRecording)
            {
                StopRecording();
                // Esperar un momento para que se detenga completamente
                System.Threading.Thread.Sleep(200);
            }

            // Si está capturando, mostrar mensaje y revertir el cambio
            if (IsCapturing())
            {
                isChangingMode = true;
                this.radioBtnSingle.Checked = false;
                this.radioBtnLive.Checked = true;
                isChangingMode = false;
                MessageBox.Show("No se puede cambiar el modo de captura mientras se está tomando una fotografía. Por favor, detenga la captura primero.", 
                    "Cambio de modo no permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar que el modo Single esté disponible
            if (!Common.canSingle)
            {
                isChangingMode = true;
                this.radioBtnSingle.Checked = false;
                this.radioBtnLive.Checked = true;
                isChangingMode = false;
                MessageBox.Show("El modo Single no está disponible para esta cámara.", 
                    "Modo no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Cambiar al modo Single
            ChangeCaptureMode(0);
        }

        /// <summary>
        /// Event handler para cuando se cambia el radio button Live
        /// </summary>
        private void radioBtnLive_CheckedChanged(object sender, EventArgs e)
        {
            // Evitar recursión si ya estamos cambiando el modo
            if (isChangingMode)
            {
                return;
            }

            // Solo procesar si el radio button está marcado y la cámara está conectada
            if (!this.radioBtnLive.Checked || !isConnect)
            {
                return;
            }

            // Si está grabando, detener grabación antes de cambiar de modo
            if (isRecording)
            {
                StopRecording();
                // Esperar un momento para que se detenga completamente
                System.Threading.Thread.Sleep(200);
            }

            // Si está capturando, mostrar mensaje y revertir el cambio
            if (IsCapturing())
            {
                isChangingMode = true;
                this.radioBtnLive.Checked = false;
                this.radioBtnSingle.Checked = true;
                isChangingMode = false;
                MessageBox.Show("No se puede cambiar el modo de captura mientras se está tomando una fotografía. Por favor, detenga la captura primero.", 
                    "Cambio de modo no permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar que el modo Live esté disponible
            if (!Common.canLive)
            {
                isChangingMode = true;
                this.radioBtnLive.Checked = false;
                this.radioBtnSingle.Checked = true;
                isChangingMode = false;
                MessageBox.Show("El modo Live no está disponible para esta cámara.", 
                    "Modo no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Cambiar al modo Live
            ChangeCaptureMode(1);
        }

        /// <summary>
        /// Event handler para cuando se cambia el radio button Recording
        /// </summary>
        private void radioBtnRecording_CheckedChanged(object sender, EventArgs e)
        {
            // Evitar recursión si ya estamos cambiando el modo
            if (isChangingMode)
            {
                return;
            }

            // Solo procesar si el radio button está marcado y la cámara está conectada
            if (!this.radioBtnRecording.Checked || !isConnect)
            {
                return;
            }

            // Si está grabando, detener grabación antes de cambiar de modo
            if (isRecording)
            {
                StopRecording();
                // Esperar un momento para que se detenga completamente
                System.Threading.Thread.Sleep(200);
            }

            // Si está capturando (Single mode), mostrar mensaje y revertir el cambio
            if (IsCapturing())
            {
                isChangingMode = true;
                this.radioBtnRecording.Checked = false;
                this.radioBtnSingle.Checked = true;
                isChangingMode = false;
                MessageBox.Show("No se puede cambiar el modo mientras se está capturando. Por favor, detenga la captura primero.", 
                    "Cambio de modo no permitido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar que haya carpeta de guardado seleccionada
            if (string.IsNullOrEmpty(saveFolderPath))
            {
                isChangingMode = true;
                this.radioBtnRecording.Checked = false;
                this.radioBtnSingle.Checked = true;
                isChangingMode = false;
                MessageBox.Show("Por favor seleccione una carpeta de guardado antes de usar el modo Recording.", 
                    "Carpeta no seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mostrar ventana de configuración de grabación
            RecordingConfigForm configForm = new RecordingConfigForm();
            if (configForm.ShowDialog() == DialogResult.OK && configForm.DialogResultOK)
            {
                recordingConfig = configForm;
                
                // Crear carpeta de sesión de grabación
                DateTime sessionStartTime = DateTime.UtcNow;
                string sessionFolderName = configForm.DestinationName + "_" + sessionStartTime.ToString("yyyyMMdd_HHmmss");
                recordingSessionFolder = Path.Combine(saveFolderPath, sessionFolderName);
                
                try
                {
                    if (!Directory.Exists(recordingSessionFolder))
                    {
                        Directory.CreateDirectory(recordingSessionFolder);
                        Console.WriteLine("Carpeta de sesión de grabación creada: {0}", recordingSessionFolder);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al crear carpeta de sesión: " + ex.Message, 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isChangingMode = true;
                    this.radioBtnRecording.Checked = false;
                    this.radioBtnSingle.Checked = true;
                    isChangingMode = false;
                    return;
                }

                // Iniciar grabación
                StartRecording();
            }
            else
            {
                // Usuario canceló, volver a Single mode
                isChangingMode = true;
                this.radioBtnRecording.Checked = false;
                this.radioBtnSingle.Checked = true;
                isChangingMode = false;
            }
        }

        /****************************************************************************/
        /******************************** Read Mode *********************************/
        /****************************************************************************/
        private void cbxReadMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSetupUI == false)
            {
                if (this.radioBtnLive.Checked)
                {
                    int ret = -1;
                    quit = true;

                    while (hasquit != true)
                    {
                        Application.DoEvents();
                    }

                    if (Common.canLive == true)
                        threadShowLiveImage.Abort();
                    else
                        threadShowSingleImage.Abort();

                    if (Common.canLive == true)
                        ret = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);
                    else
                        ret = ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);


                    Common.camReadMode = (uint)this.comBoxReadMode.SelectedIndex;
                    Console.WriteLine("camReadMode = {0}", Common.camReadMode);
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
                    retVal = ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
                    if (this.checkBoxOS.Checked)
                    {
                        ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                        Common.camImageStartX = Common.camEFStartX;
                        Common.camImageStartY = Common.camEFStartY;
                        Common.camImageSizeX = Common.camEFSizeX;
                        Common.camImageSizeY = Common.camEFSizeY / 2 * 2;
                    }
                    else
                    {
                        Common.camImageStartX = 0;
                        Common.camImageStartY = 0;
                        Common.camImageSizeX  = Common.camMinImageWidth / Common.camBinX;
                        Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
                    }
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);

                    quit = false;
                    hasquit = false;

                    if (Common.canLive == true) {
                        ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(Common.camHandle);

                        threadShowLiveImage = new Thread(new ParameterizedThreadStart(ShowLiveImage));
                        threadShowLiveImage.Start();
                    } else {
                        threadShowSingleImage = new Thread(new ParameterizedThreadStart(ShowSingleImage));
                        threadShowSingleImage.Start();
                    }
                }
                else
                {
                    ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);

                    Common.camReadMode = (uint)this.comBoxReadMode.SelectedIndex;
                    Console.WriteLine("camReadMode = {0}", Common.camReadMode);
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
                    retVal = ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
                    if (this.checkBoxOS.Checked)
                    {
                        ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                        Common.camImageStartX = Common.camEFStartX;
                        Common.camImageStartY = Common.camEFStartY;
                        Common.camImageSizeX  = Common.camEFSizeX;
                        Common.camImageSizeY  = Common.camEFSizeY / 2 * 2;
                    }
                    else
                    {
                        Common.camImageStartX = 0;
                        Common.camImageStartY = 0;
                        Common.camImageSizeX  = Common.camMinImageWidth / Common.camBinX;
                        Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
                    }
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);
                }
            }
        }

        /****************************************************************************/
        /******************************** Bits Mode *********************************/
        /****************************************************************************/
        private void comBoxFileFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comBoxFileFormat.SelectedItem != null)
            {
                Common.imageFileFormat = this.comBoxFileFormat.SelectedItem.ToString();
                Console.WriteLine("Formato de archivo seleccionado: {0}", Common.imageFileFormat);
            }
        }

        private void comBoxBits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSetupUI == false)
            {
                if (this.radioBtnLive.Checked)
                {
                    int ret = -1;
                    quit = true;

                    while (hasquit != true)
                    {
                        Application.DoEvents();
                    }

                    if (Common.canLive == true)
                        threadShowLiveImage.Abort();
                    else
                        threadShowSingleImage.Abort();

                    if (Common.canLive == true)
                        ret = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);
                    else
                        ret = ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);


                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
                    ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
                    if (this.checkBoxOS.Checked)
                    {
                        ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                        Common.camImageStartX = Common.camEFStartX;
                        Common.camImageStartY = Common.camEFStartY;
                        Common.camImageSizeX  = Common.camEFSizeX;
                        Common.camImageSizeY  = Common.camEFSizeY / 2 * 2;
                    }
                    else
                    {
                        Common.camImageStartX = 0;
                        Common.camImageStartY = 0;
                        Common.camImageSizeX  = Common.camMinImageWidth / Common.camBinX;
                        Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
                    }
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);

                    if (this.comBoxBits.Text == "RAW8") {
                        Common.camImageBits = 8;
                        Common.camColorOnOff = false;
                    } else if (this.comBoxBits.Text == "RGB24") {
                        Common.camImageBits = 24;
                        Common.camColorOnOff = true;
                    } else if (this.comBoxBits.Text == "RAW16") {
                        Common.camImageBits = 16;
                        Common.camColorOnOff = false;
                    }
                    Console.WriteLine("Text = {0} Bits = {1} Color = {2}", this.comBoxBits.Text, Common.camImageBits, Common.camColorOnOff);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);

                    quit = false;
                    hasquit = false;

                    if (Common.canLive == true)
                    {
                        ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(Common.camHandle);

                        threadShowLiveImage = new Thread(new ParameterizedThreadStart(ShowLiveImage));
                        threadShowLiveImage.Start();
                    }
                    else
                    {
                        threadShowSingleImage = new Thread(new ParameterizedThreadStart(ShowSingleImage));
                        threadShowSingleImage.Start();
                    }
                }
                else
                {
                    ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
                    ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
                    if (this.checkBoxOS.Checked)
                    {
                        ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                        Common.camImageStartX = Common.camEFStartX;
                        Common.camImageStartY = Common.camEFStartY;
                        Common.camImageSizeX  = Common.camEFSizeX;
                        Common.camImageSizeY  = Common.camEFSizeY / 2 * 2;
                    }
                    else
                    {
                        Common.camImageStartX = 0;
                        Common.camImageStartY = 0;
                        Common.camImageSizeX  = Common.camMinImageWidth / Common.camBinX;
                        Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
                    }
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);

                    if (this.comBoxBits.Text == "RAW8") {
                        Common.camImageBits = 8;
                        Common.camColorOnOff = false;
                    } else if (this.comBoxBits.Text == "RGB24") {
                        Common.camImageBits = 24;
                        Common.camColorOnOff = true;
                    } else if (this.comBoxBits.Text == "RAW16") {
                        Common.camImageBits = 16;
                        Common.camColorOnOff = false;
                    }
                    Console.WriteLine("Text = {0} Bits = {1} Color = {2}", this.comBoxBits.Text, Common.camImageBits, Common.camColorOnOff);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);
                }
            }
        }

        /****************************************************************************/
        /********************************* Rin Mode *********************************/
        /****************************************************************************/
        private void comBoxBinMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSetupUI == false)
            {
                if (this.radioBtnLive.Checked)
                {
                    int ret = -1;
                    quit = true;

                    while (hasquit != true)
                    {
                        Application.DoEvents();
                    }

                    Console.WriteLine("BIN : live = {0}", Common.canLive);
                    if (Common.canLive == true)
                        threadShowLiveImage.Abort();
                    else
                        threadShowSingleImage.Abort();

                    if (Common.canLive == true)
                        ret = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);
                    else
                        ret = ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);

                    Console.WriteLine("BIN : ReadMode = {0}", Common.camReadMode);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);
                    Console.WriteLine("BIN : Stream Mode = {0}", Common.camReadMode);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
                    ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    if (this.comBoxBinMode.Text == "1X1") {
                        Common.camBinX = 1;
                        Common.camBinY = 1;
                    } else if (this.comBoxBinMode.Text == "2X2") {
                        Common.camBinX = 2;
                        Common.camBinY = 2;
                    } else if (this.comBoxBinMode.Text == "3X3") {
                        Common.camBinX = 3;
                        Common.camBinY = 3;
                    } else if (this.comBoxBinMode.Text == "4X4") {
                        Common.camBinX = 4;
                        Common.camBinY = 4;
                    } else if (this.comBoxBinMode.Text == "6X6")
                    {
                        Common.camBinX = 6;
                        Common.camBinY = 6;
                    } else if (this.comBoxBinMode.Text == "8X8")
                    {
                        Common.camBinX = 8;
                        Common.camBinY = 8;
                    }

                    Console.WriteLine("BIN : camBinX = {0} camBinY = {1}", Common.camBinX, Common.camBinY);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
                    if (this.checkBoxOS.Checked)
                    {
                        ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                        Common.camImageStartX = Common.camEFStartX;
                        Common.camImageStartY = Common.camEFStartY;
                        Common.camImageSizeX  = Common.camEFSizeX;
                        Common.camImageSizeY  = Common.camEFSizeY / 2 * 2;
                    }
                    else
                    {
                        Common.camImageStartX = 0;
                        Common.camImageStartY = 0;
                        Common.camImageSizeX  = Common.camMinImageWidth  / Common.camBinX;
                        Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
                    }
                    Console.WriteLine("BIN : x = {0} y = {1} sizex = {2} sizey = {3}", Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);

                    Console.WriteLine("BIN : Bits = {0}", Common.camImageBits);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);
                    Console.WriteLine("BIN : Color = {0}", Common.camColorOnOff);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

                    Console.WriteLine("BIN : camExpTime = {0}", Common.camExpTime);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);
                    Console.WriteLine("BIN : camGain = {0}", Common.camGain);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);
                    Console.WriteLine("BIN : camOffset = {0}", Common.camOffset);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);
                    Console.WriteLine("BIN : camTraffic = {0}", Common.camTraffic);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);

                    quit = false;
                    hasquit = false;

                    if (Common.canLive == true)
                    {
                        ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(Common.camHandle);

                        threadShowLiveImage = new Thread(new ParameterizedThreadStart(ShowLiveImage));
                        threadShowLiveImage.Start();
                    }
                    else
                    {
                        threadShowSingleImage = new Thread(new ParameterizedThreadStart(ShowSingleImage));
                        threadShowSingleImage.Start();
                    }
                }
                else
                {
                    ASCOM.QHYCCD.libqhyccd.CancelQHYCCDExposingAndReadout(Common.camHandle);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);
                    Console.WriteLine("ReadMode = {0}", Common.camReadMode);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);
                    Console.WriteLine("Stream Mode = {0}", Common.camReadMode);
                    ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    if (this.comBoxBinMode.Text == "1X1") {
                        Common.camBinX = 1;
                        Common.camBinY = 1;
                    } else if (this.comBoxBinMode.Text == "2X2") {
                        Common.camBinX = 2;
                        Common.camBinY = 2;
                    } else if (this.comBoxBinMode.Text == "3X3") {
                        Common.camBinX = 3;
                        Common.camBinY = 3;
                    } else if (this.comBoxBinMode.Text == "4X4") {
                        Common.camBinX = 4;
                        Common.camBinY = 4;
                    } else if (this.comBoxBinMode.Text == "6X6") {
                        Common.camBinX = 6;
                        Common.camBinY = 6;
                    } else if (this.comBoxBinMode.Text == "8X8") {
                        Common.camBinX = 8;
                        Common.camBinY = 8;
                    }

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);
                    if (this.checkBoxOS.Checked)
                    {
                        ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                        Common.camImageStartX = Common.camEFStartX;
                        Common.camImageStartY = Common.camEFStartY;
                        Common.camImageSizeX  = Common.camEFSizeX;
                        Common.camImageSizeY  = Common.camEFSizeY / 2 * 2;
                    }
                    else
                    {
                        Common.camImageStartX = 0;
                        Common.camImageStartY = 0;
                        Common.camImageSizeX  = Common.camMinImageWidth / Common.camBinX;
                        Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
                    }
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);
                    Console.WriteLine("Text = {0} BinX = {1} BinY = {2}", this.comBoxBinMode.Text, Common.camBinX, Common.camBinY);
                    Console.WriteLine("StartX = {0} StartY = {1} SizeX = {2} SizeY = {3}", Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);
                    Console.WriteLine("Bits = {0}", Common.camImageBits);
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);
                    Console.WriteLine("Color = {0}", Common.camColorOnOff);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);

                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);
                }
            }
        }

        /*********************************************************/
        /************************* Exposure **********************/
        /*********************************************************/
        private void trackBarExp_Scroll(object sender, EventArgs e)
        {
            this.textBoxExp.Text = this.trackBarExp.Value.ToString();

            if (this.comBoxUnit.Text == "1~1000 us")
            {
                Common.camExpTime = this.trackBarExp.Value;
            }
            else if (this.comBoxUnit.Text == "1~1000 ms")
            {
                Common.camExpTime = this.trackBarExp.Value * 1000f;
            }
            else if (this.comBoxUnit.Text == "1~1000 s")
            {
                Common.camExpTime = this.trackBarExp.Value * 1000000f;
            }

            Console.WriteLine("ExpTime = {0}", Common.camExpTime);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);
        }

        private void textBoxExp_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxExp_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar != 8 && e.KeyChar != 13 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBoxExp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Convert.ToUInt32(this.textBoxExp.Text) > 1000)
                    this.textBoxExp.Text = "1000";
                if (Convert.ToUInt32(this.textBoxExp.Text) < 1)
                    this.textBoxExp.Text = "1";

                this.trackBarExp.Value = Convert.ToInt32(this.textBoxExp.Text);

                if (this.comBoxUnit.Text == "1~1000 us")
                    Common.camExpTime = this.trackBarExp.Value;
                else if (this.comBoxUnit.Text == "1~1000 ms")
                    Common.camExpTime = this.trackBarExp.Value * 1000f;
                else if (this.comBoxUnit.Text == "1~1000 s")
                    Common.camExpTime = this.trackBarExp.Value * 1000000f;

                Console.WriteLine("ExpTime = {0}", Common.camExpTime);
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isSetupUI)
            {
                if (this.comBoxUnit.Text == "1~1000 us")
                {
                    Common.camExpTime = this.trackBarExp.Value;
                }
                else if (this.comBoxUnit.Text == "1~1000 ms")
                {
                    Common.camExpTime = this.trackBarExp.Value * 1000f;
                }
                else if (this.comBoxUnit.Text == "1~1000 s")
                {
                    Common.camExpTime = this.trackBarExp.Value * 1000000f;
                }

                Console.WriteLine("ExpTime = {0}", Common.camExpTime);
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);
            }
        }

        /*************************************************************/
        /************************** Gain *****************************/
        /*************************************************************/
        private void testBoxGain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && e.KeyChar != 13 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void testBoxGain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Convert.ToDouble(this.testBoxGain.Text) > this.trackBarGain.Maximum)
                {
                    this.trackBarGain.Value = this.trackBarGain.Maximum;
                    this.testBoxGain.Text = this.trackBarGain.Maximum.ToString();
                }
                else if (Convert.ToDouble(this.testBoxGain.Text) < this.trackBarGain.Minimum)
                {
                    this.trackBarGain.Value = this.trackBarGain.Minimum;
                    this.testBoxGain.Text = this.trackBarGain.Minimum.ToString();
                }
                else
                {
                    this.trackBarGain.Value = Convert.ToInt32(this.testBoxGain.Text);
                }

                Common.camGain = (double)this.trackBarGain.Value;
                //if (this.radioBtnLive.Checked == true)
                //{
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);
                //}
            }
        }

        private void testBoxGain_TextChanged(object sender, EventArgs e)
        {

        }
        
        private void trackBarGain_Scroll(object sender, EventArgs e)
        {
            Common.camGain = (double)this.trackBarGain.Value;
            this.testBoxGain.Text = this.trackBarGain.Value.ToString();

            //if (this.radioBtnLive.Checked == true)
            //{
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_GAIN, Common.camGain);
            //}
        }

        /*********************************************************************/
        /********************************* Offset ****************************/
        /*********************************************************************/
        private void textBoxOffset_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && e.KeyChar != 13 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBoxOffset_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Convert.ToDouble(this.textBoxOffset.Text) > this.trackBarOffset.Maximum)
                {
                    this.trackBarOffset.Value = this.trackBarOffset.Maximum;
                    this.textBoxOffset.Text = this.trackBarOffset.Maximum.ToString();
                }
                else if (Convert.ToDouble(this.textBoxOffset.Text) < this.trackBarOffset.Minimum)
                {
                    this.trackBarOffset.Value = this.trackBarOffset.Minimum;
                    this.textBoxOffset.Text = this.trackBarOffset.Minimum.ToString();
                }
                else
                {
                    this.trackBarOffset.Value = Convert.ToInt32(this.textBoxOffset.Text);
                }

                Common.camOffset = (double)this.trackBarOffset.Value;
                //if (this.radioBtnLive.Checked == true)
                //{
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);
                //}
            }
        }

        private void textBoxOffset_TextChanged(object sender, EventArgs e)
        {

        }

        private void trackBarOffset_Scroll(object sender, EventArgs e)
        {
            Common.camOffset = (double)this.trackBarOffset.Value;
            this.textBoxOffset.Text = this.trackBarOffset.Value.ToString();

            //if (this.radioBtnLive.Checked == true)
            //{
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_OFFSET, Common.camOffset);
            //}
        }

        /********************************************************************/
        /****************************** Traffic *****************************/
        /********************************************************************/
        private void textBoxUSBTraffic_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && e.KeyChar != 13 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBoxUSBTraffic_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //Console.WriteLine("Text = {0} Maximum = {1} Minimum = {2}", this.textBoxUSBTraffic.Text, this.trackBarUSBTraffic.Maximum, this.trackBarUSBTraffic.Minimum);
                if (Convert.ToDouble(this.textBoxUSBTraffic.Text) > this.trackBarUSBTraffic.Maximum)
                {
                    this.trackBarUSBTraffic.Value = this.trackBarUSBTraffic.Maximum;
                    this.textBoxUSBTraffic.Text = this.trackBarUSBTraffic.Maximum.ToString();
                }
                else if (Convert.ToDouble(this.textBoxUSBTraffic.Text) < this.trackBarUSBTraffic.Minimum)
                {
                    this.trackBarUSBTraffic.Value = this.trackBarUSBTraffic.Minimum;
                    this.textBoxUSBTraffic.Text = this.trackBarUSBTraffic.Minimum.ToString();
                }
                else
                {
                    this.trackBarUSBTraffic.Value = Convert.ToInt32(this.textBoxUSBTraffic.Text);
                }

                Common.camTraffic = (double)this.trackBarUSBTraffic.Value;
                //if (this.radioBtnLive.Checked == true)
                //{
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);
                //}
            }
        }

        private void textBoxUSBTraffic_TextChanged(object sender, EventArgs e)
        {

        }
        
        private void trackBarUSBTraffic_Scroll(object sender, EventArgs e)
        {
            Common.camTraffic = (double)this.trackBarUSBTraffic.Value;
            this.textBoxUSBTraffic.Text = this.trackBarUSBTraffic.Value.ToString();

            //if (this.radioBtnLive.Checked == true)
            //{
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_USBTRAFFIC, Common.camTraffic);
            //}
        }


        /**********************************************************/
        /*************************** GPS **************************/
        /**********************************************************/

        private void btnGPSOnOff_Click(object sender, EventArgs e)
        {
            if (this.btnGPSOnOff.Text == "ON")
            {
                retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CAM_GPS, 1);
                this.btnGPSOnOff.Text = "OFF";
            }
            else
            {
                retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CAM_GPS, 0);
                this.btnGPSOnOff.Text = "OFF";
            }
        }


        private void btnLEDOnOff_Click(object sender, EventArgs e)
        {
            if (this.btnLEDOnOff.Text == "LEDON")
            {
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
                this.btnLEDOnOff.Text = "LEDOFF";
            }
            else
            {
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 0);
                this.btnLEDOnOff.Text = "LEDON";
            }
        }

        private void textBoxVCOX_TextChanged(object sender, EventArgs e)
        {
            this.trackBarVCOX.Value = Convert.ToInt32(this.textBoxVCOX.Text);

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSVCOXFreq(Common.camHandle, (uint)this.trackBarVCOX.Value);
        }

        private void trackBarVCOX_Scroll(object sender, EventArgs e)
        {
            this.textBoxVCOX.Text = this.trackBarVCOX.Value.ToString();

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSVCOXFreq(Common.camHandle, (uint)this.trackBarVCOX.Value);
        }

        private void numUDPA9_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA9.Value > 9)
                this.numUDPA9.Value = 0;
            if (this.numUDPA9.Value < 0)
                this.numUDPA9.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA8_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA8.Value > 9)
                this.numUDPA8.Value = 0;
            if (this.numUDPA8.Value < 0)
                this.numUDPA8.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA7_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA7.Value > 9)
                this.numUDPA7.Value = 0;
            if (this.numUDPA7.Value < 0)
                this.numUDPA7.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA6_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA6.Value > 9)
                this.numUDPA6.Value = 0;
            if (this.numUDPA6.Value < 0)
                this.numUDPA6.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA5_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA5.Value > 9)
                this.numUDPA5.Value = 0;
            if (this.numUDPA5.Value < 0)
                this.numUDPA5.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA4_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA4.Value > 9)
                this.numUDPA4.Value = 0;
            if (this.numUDPA4.Value < 0)
                this.numUDPA4.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA3_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA3.Value > 9)
                this.numUDPA3.Value = 0;
            if (this.numUDPA3.Value < 0)
                this.numUDPA3.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA2_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA2.Value > 9)
                this.numUDPA2.Value = 0;
            if (this.numUDPA2.Value < 0)
                this.numUDPA2.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPA1_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPA1.Value > 9)
                this.numUDPA1.Value = 0;
            if (this.numUDPA1.Value < 0)
                this.numUDPA1.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPA9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPA8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPA7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPA6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPA5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPA4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPA3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPA2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPA1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSA(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 2);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB9_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB9.Value > 9)
                this.numUDPB9.Value = 0;
            if (this.numUDPB9.Value < 0)
                this.numUDPB9.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);

        }

        private void numUDPB8_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB8.Value > 9)
                this.numUDPB8.Value = 0;
            if (this.numUDPB8.Value < 0)
                this.numUDPB8.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB7_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB7.Value > 9)
                this.numUDPB7.Value = 0;
            if (this.numUDPB7.Value < 0)
                this.numUDPB7.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB6_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB6.Value > 9)
                this.numUDPB6.Value = 0;
            if (this.numUDPB6.Value < 0)
                this.numUDPB6.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB5_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB5.Value > 9)
                this.numUDPB5.Value = 0;
            if (this.numUDPB5.Value < 0)
                this.numUDPB5.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB4_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB4.Value > 9)
                this.numUDPB4.Value = 0;
            if (this.numUDPB4.Value < 0)
                this.numUDPB4.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB3_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB3.Value > 9)
                this.numUDPB3.Value = 0;
            if (this.numUDPB3.Value < 0)
                this.numUDPB3.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB2_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB2.Value > 9)
                this.numUDPB2.Value = 0;
            if (this.numUDPB2.Value < 0)
                this.numUDPB2.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        private void numUDPB1_ValueChanged(object sender, EventArgs e)
        {
            if (this.numUDPB1.Value > 9)
                this.numUDPB1.Value = 0;
            if (this.numUDPB1.Value < 0)
                this.numUDPB1.Value = 9;

            uint value = Convert.ToUInt32(this.numUDPB9.Value) * 100000000 +
                         Convert.ToUInt32(this.numUDPB8.Value) * 10000000 +
                         Convert.ToUInt32(this.numUDPB7.Value) * 1000000 +
                         Convert.ToUInt32(this.numUDPB6.Value) * 100000 +
                         Convert.ToUInt32(this.numUDPB5.Value) * 10000 +
                         Convert.ToUInt32(this.numUDPB4.Value) * 1000 +
                         Convert.ToUInt32(this.numUDPB3.Value) * 100 +
                         Convert.ToUInt32(this.numUDPB2.Value) * 10 +
                         Convert.ToUInt32(this.numUDPB1.Value) * 1;

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSPOSB(Common.camHandle, 0, value, 40);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCalMode(Common.camHandle, 1);
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDGPSLedCal(Common.camHandle, value, 40);
        }

        /**************************************************************************/
        /************************************ CFW *********************************/
        /**************************************************************************/
        private void comBoxCFWPos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitCFW == false)
            {
                this.comBoxCFWPos.Enabled = false;

                double targetCFWPos = (double)this.comBoxCFWPos.SelectedIndex + 48.0;
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT, targetCFWPos);

                curCFWPos = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT);
                while (curCFWPos != targetCFWPos)
                {
                    this.labelCFWStatus.Text = "Moving";
                    Application.DoEvents();

                    curCFWPos = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CFWPORT);
                }

                this.comBoxCFWPos.Enabled = true;
                this.labelCFWStatus.Text = "IDLE";
            }
        }

        /**************************************************************************/
        /********************************** Cooler ********************************/
        /**************************************************************************/
        delegate void ControlTEMPAndPWMCallBack();
        private void ControlTEMPAndPWM()
        {
            int ret = -1;
            double target = 0.0;
            double curTEMP = 0.0;
            double curPWM = 0.0;
            double humidity = 0.0;
            double pressure = 0.0;
            //double curVoltage = 0.0;

            if (controlCooler == true)
            {
                controlCooler = !controlCooler;

                //Console.WriteLine("Text = {0}", this.btnCoolerOnOff.Text);
                if (this.btnCoolerOnOff.Text == "OFF")
                {
                    if (this.btnCoolerMode.Text == "Auto Mode")
                    {
                        target = (double)this.trackBarCooler.Value;
                        curPWM = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CURPWM);
                        if (curPWM < 0.0) curPWM = 0.0;
                        int subPWM = (int)((curPWM / 255.0 * 100.0 - (int)(curPWM / 255.0 * 100.0)) * 10);
                        this.labelNowPWM.Text = ((int)(curPWM / 255.0 * 100.0)).ToString() + "." + subPWM.ToString() + " %";

                        Console.WriteLine("Cooler Target : {0}", target);
                        retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_COOLER, target);
                    }
                    else
                    {
                        target = (double)this.trackBarCooler.Value / 100.0 * 255;
                        retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_MANULPWM, target);
                    }
                }
            }
            else
            {
                controlCooler = !controlCooler;

                curTEMP = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CURTEMP);
                int subTEMP = (int)((curTEMP - (int)curTEMP) * 10);
                if (subTEMP < 0) subTEMP = subTEMP * -1;
                this.labelNowTemp.Text = ((int)curTEMP).ToString() + "." + subTEMP.ToString() + " ℃";
                //Console.WriteLine("curTEMP = {0}", curTEMP);

                if (Common.canHumidity)
                {
                    ASCOM.QHYCCD.libqhyccd.GetQHYCCDHumidity(Common.camHandle, ref humidity);
                    this.labelHumidity.Text = humidity.ToString() + " %";
                }
                if (Common.canPressure)
                {
                    ASCOM.QHYCCD.libqhyccd.GetQHYCCDPressure(Common.camHandle, ref pressure);
                    this.labelPressure.Text = pressure.ToString() + " mbar";
                }

                //Console.WriteLine("humidity = {0} pressure = {1}", humidity, pressure);
            }
        }

        unsafe void ControlCooler(object obj)
        {
            while (quitCoolerGet != true)
            {
                ControlTEMPAndPWMCallBack deg = new ControlTEMPAndPWMCallBack(ControlTEMPAndPWM);
                this.Invoke(deg);

                System.Threading.Thread.Sleep(1000);
            }

            hasQuitCoolerGet = true;
        }

        private void btnCoolerOnOff_Click(object sender, EventArgs e)
        {
            int ret = -1;

            if (this.btnCoolerOnOff.Text == "ON")
            {
                this.btnCoolerOnOff.Text = "OFF";
                this.groupBoxCooler.Enabled = true;
                
                this.btnCoolerMode.Enabled = true;
                this.btnCoolerMode.Text = "Auto Mode";
                
                this.trackBarCooler.Minimum = -30;
                this.trackBarCooler.Maximum = 30;
                this.trackBarCooler.Value = 0;
                
                this.labelCooler.Text = this.trackBarCooler.Value.ToString() + " °C";
            }
            else
            {
                this.btnCoolerOnOff.Text = "ON";
                this.groupBoxCooler.Enabled = false;
                
                this.btnCoolerMode.Enabled = false;
                this.btnCoolerMode.Text = "Auto Mode";
                this.labelNowPWM.Text = "- %";
                
                this.trackBarCooler.Minimum = -30;
                this.trackBarCooler.Maximum = 30;
                this.trackBarCooler.Value = 0;
                
                this.labelCooler.Text = this.trackBarCooler.Value.ToString() + " °C";

                ret = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_MANULPWM, 0.0);
            }
        }

        private void btnCoolerMode_Click(object sender, EventArgs e)
        {
            if (this.btnCoolerMode.Text == "Auto Mode")
            {
                this.btnCoolerMode.Text = "Manual Mode";
                this.trackBarCooler.Minimum = 0;
                this.trackBarCooler.Maximum = 100;

                double curPWM = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CURPWM);
                this.trackBarCooler.Value = (int)(curPWM / 255 * 100);
                this.labelCooler.Text = this.trackBarCooler.Value.ToString() + " %";
                //Console.WriteLine("btnCoolerMode Click Manual Mode value = {0}", this.trackBarCooler.Value);
            }
            else
            {
                this.btnCoolerMode.Text = "Auto Mode";
                this.trackBarCooler.Minimum = -30;
                this.trackBarCooler.Maximum = 30;

                double curTEMP = ASCOM.QHYCCD.libqhyccd.GetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_CURTEMP);
                this.trackBarCooler.Value = (int)curTEMP;
                this.labelCooler.Text = this.trackBarCooler.Value.ToString() + " °C";
                //Console.WriteLine("btnCoolerMode Click Auto Mode value = {0}", this.trackBarCooler.Value);
            }
        }

        private void trackBarCooler_Scroll(object sender, EventArgs e)
        {
            if (this.btnCoolerMode.Text == "Auto Mode")
            {
                this.labelCooler.Text = this.trackBarCooler.Value.ToString() + " °C";
                //Console.WriteLine("trackBarCooler Scroll Auto Mode value = {0}", this.trackBarCooler.Value);
            }
            else
            {
                this.labelCooler.Text = this.trackBarCooler.Value.ToString() + " %";
                //Console.WriteLine("trackBarCooler Scroll Manual Mode value = {0}", this.trackBarCooler.Value);
            }
        }

        private void checkBoxOS_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBoxOS.Checked)
            {
                retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDEffectiveArea(Common.camHandle, ref Common.camEFStartX, ref Common.camEFStartY, ref Common.camEFSizeX, ref Common.camEFSizeY);
                Common.camImageStartX = Common.camEFStartX;
                Common.camImageStartY = Common.camEFStartY;
                Common.camImageSizeX  = Common.camEFSizeX;
                Common.camImageSizeY  = Common.camEFSizeY / 2 * 2;
            }
            else
            {
                Common.camImageStartX = 0;
                Common.camImageStartY = 0;
                Common.camImageSizeX  = Common.camMinImageWidth  / Common.camBinX;
                Common.camImageSizeY  = Common.camMinImageHeight / Common.camBinY / 2 * 2;
            }
            retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageSizeX, Common.camImageSizeY);
        }

        
        delegate void CSharpDelegateIn(StringBuilder id);

        void pnp_Event_In_Func(StringBuilder id)
        {
            Console.WriteLine("Camera In");
            if (!isSetupUI)
            {
                if(this.radioBtnLive.Checked)
                {
                    System.Threading.Thread.Sleep(500);

                    while (isConnect != false)
                    {
                        Application.DoEvents();
                    }

                    Common.camHandle = ASCOM.QHYCCD.libqhyccd.OpenQHYCCD(Common.camID);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDReadMode(Common.camHandle, Common.camReadMode);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(Common.camHandle, Common.camStreamMode);

                    retVal = ASCOM.QHYCCD.libqhyccd.InitQHYCCD(Common.camHandle);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(Common.camHandle, Common.camBinX, Common.camBinY);

                    //retVal = ASCOM.QHYCCD.libqhyccd.GetQHYCCDReadModeResolution(Common.camHandle, Common.camReadMode, ref Common.camImageWidth, ref Common.camImageHeight);
                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(Common.camHandle, Common.camImageStartX, Common.camImageStartY, Common.camImageWidth, Common.camImageHeight);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_TRANSFERBIT, Common.camImageBits);

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(Common.camHandle, Common.camColorOnOff);

                    isConnect = true;

                    retVal = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(Common.camHandle, CONTROL_ID.CONTROL_EXPOSURE, Common.camExpTime);

                    quit = false;
                    hasquit = false;

                    this.btnStartCap.Enabled = false;
                    this.btnStopCap.Enabled = true;

                    ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(Common.camHandle);
                
                    threadShowLiveImage = new Thread(new ParameterizedThreadStart(ShowLiveImage));
                    threadShowLiveImage.Start();
                }
            }
        }

        CSharpDelegateIn csharpDelegateIn;

        void InitRegisterPnpEventIn()
        {
            csharpDelegateIn = new CSharpDelegateIn(pnp_Event_In_Func);
            ASCOM.QHYCCD.libqhyccd.RegisterPnpEventIn(Marshal.GetFunctionPointerForDelegate(csharpDelegateIn));
        }

        delegate void CSharpDelegateOut(StringBuilder id);

        void pnp_Event_Out_Func(StringBuilder id)
        {
            Console.WriteLine("Camera Out");
            if (!isSetupUI)
            {
                if (this.radioBtnLive.Checked)
                {
                    int ret = -1;
                    quit = true;

                    while (hasquit != true)
                    {
                        Application.DoEvents();
                    }

                    if (Common.canLive == true)
                        threadShowLiveImage.Abort();
                    else
                        threadShowSingleImage.Abort();

                    retVal = ASCOM.QHYCCD.libqhyccd.StopQHYCCDLive(Common.camHandle);

                    retVal = ASCOM.QHYCCD.libqhyccd.CloseQHYCCD(Common.camHandle);

                    isConnect = false;

                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        CSharpDelegateOut csharpDelegateOut;

        void InitRegisterPnpEventOut()
        {
            csharpDelegateOut = new CSharpDelegateOut(pnp_Event_Out_Func);
            ASCOM.QHYCCD.libqhyccd.RegisterPnpEventOut(Marshal.GetFunctionPointerForDelegate(csharpDelegateOut));
        }

        private void btnBurstOnOff_Click(object sender, EventArgs e)
        {
            if (this.btnBurstOnOff.Text == "Burst Is OFF")
            {
                ASCOM.QHYCCD.libqhyccd.EnableQHYCCDBurstMode(Common.camHandle, true);
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDBurstModeStartEnd(Common.camHandle, Convert.ToUInt16(this.textBoxBurstStart.Text), Convert.ToUInt16(this.textBoxBurstEnd.Text));
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDBurstModePatchNumber(Common.camHandle, Convert.ToUInt32(this.textBoxBurstPatch.Text));

                System.Threading.Thread.Sleep(100);

                this.btnBurstOnOff.Text        = "Burst Is ON";
                this.btnBurstStartEnd.Enabled  = true;
                this.textBoxBurstStart.Enabled = true;
                this.textBoxBurstEnd.Enabled   = true;
                this.btnBurstPatch.Enabled     = true;
                this.textBoxBurstPatch.Enabled = true;
                this.btnBurstCapture.Enabled   = true;

                Common.burstOnOff = true;
            }
            else if (this.btnBurstOnOff.Text == "Burst Is ON")
            {
                ASCOM.QHYCCD.libqhyccd.EnableQHYCCDBurstMode(Common.camHandle, false);

                System.Threading.Thread.Sleep(100);

                this.btnBurstOnOff.Text = "Burst Is OFF";
                this.btnBurstStartEnd.Enabled = false;
                this.textBoxBurstStart.Enabled = false;
                this.textBoxBurstEnd.Enabled = false;
                this.btnBurstPatch.Enabled = false;
                this.textBoxBurstPatch.Enabled = false;
                this.btnBurstCapture.Enabled = false;

                Common.burstOnOff = false;
            }
            else
            {

            }
        }

        private void btnBurstStartEnd_Click(object sender, EventArgs e)
        {
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBurstModeStartEnd(Common.camHandle, Convert.ToUInt16(this.textBoxBurstStart.Text), Convert.ToUInt16(this.textBoxBurstEnd.Text));
        }

        private void btnBurstPatch_Click(object sender, EventArgs e)
        {
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBurstModePatchNumber(Common.camHandle, Convert.ToUInt32(this.textBoxBurstPatch.Text));
        }

        private void btnBurstCapture_Click(object sender, EventArgs e)
        {
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBurstIDLE(Common.camHandle);
            System.Threading.Thread.Sleep(200);
            Common.burstCapNum = 0;
            ASCOM.QHYCCD.libqhyccd.ReleaseQHYCCDBurstIDLE(Common.camHandle);
            
            Common.burstCapTarget = Convert.ToUInt32(this.textBoxBurstEnd.Text) - Convert.ToUInt32(this.textBoxBurstStart.Text) - 1;
            Common.burstCap = true;
        }

        //#endregion
    }
}
    
    namespace StructModel
    {
        public enum CONTROL_ID
        {
            /*0*/
            CONTROL_BRIGHTNESS = 0, //!< image brightness
            /*1*/
            CONTROL_CONTRAST,       //!< image contrast
            /*2*/
            CONTROL_WBR,            //!< red of white balance
            /*3*/
            CONTROL_WBB,            //!< blue of white balance
            /*4*/
            CONTROL_WBG,            //!< the green of white balance
            /*5*/
            CONTROL_GAMMA,          //!< screen gamma
            /*6*/
            CONTROL_GAIN,           //!< camera gain
            /*7*/
            CONTROL_OFFSET,         //!< camera offset
            /*8*/
            CONTROL_EXPOSURE,       //!< expose time (us)
            /*9*/
            CONTROL_SPEED,          //!< transfer speed
            /*10*/
            CONTROL_TRANSFERBIT,    //!< image depth bits
            /*11*/
            CONTROL_CHANNELS,       //!< image channels
            /*12*/
            CONTROL_USBTRAFFIC,     //!< hblank
            /*13*/
            CONTROL_ROWNOISERE,     //!< row denoise
            /*14*/
            CONTROL_CURTEMP,        //!< current cmos or ccd temprature
            /*15*/
            CONTROL_CURPWM,         //!< current cool pwm
            /*16*/
            CONTROL_MANULPWM,       //!< set the cool pwm
            /*17*/
            CONTROL_CFWPORT,        //!< control camera color filter wheel port
            /*18*/
            CONTROL_COOLER,         //!< check if camera has cooler
            /*19*/
            CONTROL_ST4PORT,        //!< check if camera has st4port
            /*20*/
            CAM_COLOR,              /// FIXME!  CAM_IS_COLOR CAM_COLOR conflict
            /*21*/
            CAM_BIN1X1MODE,         //!< check if camera has bin1x1 mode
            /*22*/
            CAM_BIN2X2MODE,         //!< check if camera has bin2x2 mode
            /*23*/
            CAM_BIN3X3MODE,         //!< check if camera has bin3x3 mode
            /*24*/
            CAM_BIN4X4MODE,         //!< check if camera has bin4x4 mode
            /*25*/
            CAM_MECHANICALSHUTTER,                   //!< mechanical shutter
            /*26*/
            CAM_TRIGER_INTERFACE,                    //!< check if camera has triger interface
            /*27*/
            CAM_TECOVERPROTECT_INTERFACE,            //!< tec overprotect
            /*28*/
            CAM_SINGNALCLAMP_INTERFACE,              //!< singnal clamp
            /*29*/
            CAM_FINETONE_INTERFACE,                  //!< fine tone
            /*30*/
            CAM_SHUTTERMOTORHEATING_INTERFACE,       //!< shutter motor heating
            /*31*/
            CAM_CALIBRATEFPN_INTERFACE,              //!< calibrated frame
            /*32*/
            CAM_CHIPTEMPERATURESENSOR_INTERFACE,     //!< chip temperaure sensor
            /*33*/
            CAM_USBREADOUTSLOWEST_INTERFACE,         //!< usb readout slowest

            /*34*/
            CAM_8BITS,                               //!< 8bit depth
            /*35*/
            CAM_16BITS,                              //!< 16bit depth
            /*36*/
            CAM_GPS,                                 //!< check if camera has gps

            /*37*/
            CAM_IGNOREOVERSCAN_INTERFACE,            //!< ignore overscan area

            /*38*/
            QHYCCD_3A_AUTOBALANCE,
            /*39*/
            QHYCCD_3A_AUTOEXPOSURE,
            /*40*/
            QHYCCD_3A_AUTOFOCUS,
            /*41*/
            CONTROL_AMPV,                            //!< ccd or cmos ampv
            /*42*/
            CONTROL_VCAM,                            //!< Virtual Camera on off
            /*43*/
            CAM_VIEW_MODE,

            /*44*/
            CONTROL_CFWSLOTSNUM,         //!< check CFW slots number
            /*45*/
            IS_EXPOSING_DONE,
            /*46*/
            ScreenStretchB,
            /*47*/
            ScreenStretchW,
            /*48*/
            CONTROL_DDR,
            /*49*/
            CAM_LIGHT_PERFORMANCE_MODE,

            /*50*/
            CAM_QHY5II_GUIDE_MODE,
            /*51*/
            DDR_BUFFER_CAPACITY,
            /*52*/
            DDR_BUFFER_READ_THRESHOLD,
            /*53*/
            DefaultGain,
            /*54*/
            DefaultOffset,
            /*55*/
            OutputDataActualBits,
            /*56*/
            OutputDataAlignment,

            /*57*/
            CAM_SINGLEFRAMEMODE,
            /*58*/
            CAM_LIVEVIDEOMODE,
            /*59*/
            CAM_IS_COLOR,
            /*60*/
            hasHardwareFrameCounter,
            /*61*/
            CONTROL_MAX_ID_Error, //** No Use , last max index */
            /*62*/
            CAM_HUMIDITY,			//!<check if camera has	 humidity sensor  20191021 LYL Unified humidity function
            /*63*/
            CAM_PRESSURE,             //check if camera has pressure sensor
            /*64*/
            CONTROL_VACUUM_PUMP,        /// if camera has VACUUM PUMP
            /*65*/
            CONTROL_SensorChamberCycle_PUMP, ///air cycle pump for sensor drying
            /*66*/
            CAM_32BITS,
            /*67*/
            CAM_Sensor_ULVO_Status, /// Sensor working status [0:init  1:good  2:checkErr  3:monitorErr 8:good 9:powerChipErr]  410 461 411 600 268 [Eris board]
            /*68*/
            CAM_SensorPhaseReTrain, /// 2020,4040/PRO，6060,42PRO
            /*69*/
            CAM_InitConfigFromFlash, /// 2410 461 411 600 268 for now
            /*70*/
            CAM_TRIGER_MODE, //check if camera has multiple triger mode
            /*71*/
            CAM_TRIGER_OUT, //check if camera support triger out function
            /*72*/
            CAM_BURST_MODE, //check if camera support burst mode
            /*73*/
            CAM_SPEAKER_LED_ALARM, // for OEM-600
            /*74*/
            CAM_WATCH_DOG_FPGA, // for _QHY5III178C Celestron, SDK have to feed this dog or it go reset

            /*24*/
            CAM_BIN6X6MODE,         //!< check if camera has bin4x4 mode
            /*24*/
            CAM_BIN8X8MODE,         //!< check if camera has bin4x4 mode


            /* Do not Put Item after  CONTROL_MAX_ID !! This should be the max index of the list */
            /*Last One */
            CONTROL_MAX_ID
        };

        public enum BAYER_ID
        {
            BAYER_GB = 1,
            BAYER_GR,
            BAYER_BG,
            BAYER_RG
        };
    }

    namespace ASCOM.QHYCCD
    {

        class libqhyccd
        {
            /// <summary>
            /// Initialize and Release
            /// </summary>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "InitQHYCCDResource",
                CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 InitQHYCCDResource();

            [DllImport("qhyccd.dll", EntryPoint = "ReleaseQHYCCDResource",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 ReleaseQHYCCDResource();

            /// <summary>
            /// Scan and Connect camera
            /// </summary>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "ScanQHYCCD",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 ScanQHYCCD();

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDId",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDId(int index, StringBuilder id);

            [DllImport("qhyccd.dll", EntryPoint = "OpenQHYCCD",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern IntPtr OpenQHYCCD(StringBuilder id);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDStreamMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDStreamMode(IntPtr handle, UInt32 mode);

            [DllImport("qhyccd.dll", EntryPoint = "InitQHYCCD",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 InitQHYCCD(IntPtr handle);

            /// <summary>
            /// Close Camera
            /// </summary>
            /// <param name="handle"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "CloseQHYCCD",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 CloseQHYCCD(IntPtr handle);

            /// <summary>
            /// Bin and Resolution
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="wbin"></param>
            /// <param name="hbin"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBinMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDBinMode(IntPtr handle, UInt32 wbin, UInt32 hbin);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDResolution",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDResolution(IntPtr handle, UInt32 startx, UInt32 starty, UInt32 sizex, UInt32 sizey);

            /// <summary>
            /// Set Parameters
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="controlid"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDParam",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDParam(IntPtr handle, CONTROL_ID controlid, double value);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDMemLength",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDMemLength(IntPtr handle);

            /// <summary>
            /// Get Paramertes
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="chipw"></param>
            /// <param name="chiph"></param>
            /// <param name="imagew"></param>
            /// <param name="imageh"></param>
            /// <param name="pixelw"></param>
            /// <param name="pixelh"></param>
            /// <param name="bpp"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDChipInfo",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDChipInfo(IntPtr handle, ref double chipw, ref double chiph, ref UInt32 imagew, ref UInt32 imageh, ref double pixelw, ref double pixelh, ref UInt32 bpp);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDOverScanArea",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDOverScanArea(IntPtr handle, ref UInt32 startx, ref UInt32 starty, ref UInt32 sizex, ref UInt32 sizey);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDEffectiveArea",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDEffectiveArea(IntPtr handle, ref UInt32 startx, ref UInt32 starty, ref UInt32 sizex, ref UInt32 sizey);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDSDKVersion",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDSDKVersion(ref UInt32 year, ref UInt32 month, ref UInt32 day, ref UInt32 subday);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDFWVersion",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDFWVersion(IntPtr handle, byte* verBuf);
            public unsafe static UInt32 C_GetQHYCCDFWVersion(IntPtr handle, byte[] verBuf)
            {
                fixed (byte* pverBuf = verBuf)
                    return GetQHYCCDFWVersion(handle, pverBuf);
            }

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDCameraStatus",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDCameraStatus(IntPtr handle, byte* status);
            public unsafe static UInt32 C_GetQHYCCDCameraStatus(IntPtr handle, byte[] status)
            {
                fixed (byte* pstatus = status)
                    return GetQHYCCDCameraStatus(handle, pstatus);
            }

            //[DllImport("qhyccd2.dll", EntryPoint = "GetQHYCCDFWVersion",
            // CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            //public unsafe static extern UInt32 GetQHYCCDFWVersion2(IntPtr handle, byte* verBuf);
            //public unsafe static UInt32 C_GetQHYCCDFWVersion2(IntPtr handle, byte[] verBuf)
            //{
            //    fixed (byte* pverBuf = verBuf)
            //        return GetQHYCCDFWVersion2(handle, pverBuf);
            //}
            //[DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDParam",
            // CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            //public unsafe static extern Int32 SetQHYCCDParam(IntPtr handle, CONTROL_ID controlid, double value);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDParam",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern double GetQHYCCDParam(IntPtr handle, CONTROL_ID controlid);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDParamMinMaxStep",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDParamMinMaxStep(IntPtr handle, CONTROL_ID controlid, ref double min, ref double max, ref double step);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDCameraStatus",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDCameraStatus(IntPtr handle, byte[] status);

            [DllImport("qhyccd.dll", EntryPoint = "SendFourLine2QHYCCDInterCamOled",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SendFourLine2QHYCCDInterCamOled(IntPtr handle, char[] temp, char[] info, char[] time, char[] mode);

            /// <summary>
            /// Single Mode
            /// </summary>
            /// <param name="handle"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "ExpQHYCCDSingleFrame",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ExpQHYCCDSingleFrame(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "CancelQHYCCDExposing",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 CancelQHYCCDExposing(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "CancelQHYCCDExposingAndReadout",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 CancelQHYCCDExposingAndReadout(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDSingleFrame",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDSingleFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte* rawArray);
            public unsafe static Int32 C_GetQHYCCDSingleFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte[] rawArray)
            {
                Int32 ret;
                fixed (byte* prawArray = rawArray)
                    ret = GetQHYCCDSingleFrame(handle, ref w, ref h, ref bpp, ref channels, prawArray);
                return ret;
            }

            [DllImport("qhyccd.dll", EntryPoint = "ControlQHYCCDGuide",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ControlQHYCCDGuide(IntPtr handle, byte Direction, UInt16 PulseTime);

            [DllImport("qhyccd.dll", EntryPoint = "ControlQHYCCDTemp",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ControlQHYCCDTemp(IntPtr handle, double targettemp);

            [DllImport("qhyccd.dll", EntryPoint = "SendOrder2QHYCCDCFW",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SendOrder2QHYCCDCFW(IntPtr handle, String order, int length);

            [DllImport("qhyccd.dll", EntryPoint = "IsQHYCCDCFWPlugged",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 IsQHYCCDCFWPlugged(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "IsQHYCCDControlAvailable",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 IsQHYCCDControlAvailable(IntPtr handle, CONTROL_ID controlid);

            [DllImport("qhyccd.dll", EntryPoint = "ControlQHYCCDShutter",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ControlQHYCCDShutter(IntPtr handle, byte targettemp);
            

            //EXPORTFUNC uint32_t STDCALL GetQHYCCDCFWStatus(qhyccd_handle *handle,char *status)
            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDCFWStatus",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDCFWStatus(IntPtr handle, StringBuilder cfwStatus);
            
            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBitsMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDBitsMode(IntPtr handle, UInt32 bits);

            [DllImport("qhyccd.dll", EntryPoint = "BeginQHYCCDLive",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 BeginQHYCCDLive(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDLiveFrame",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDLiveFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte* imgdata);
            public unsafe static Int32 C_GetQHYCCDLiveFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte[] imgdata)
            {
                Int32 ret;
                fixed (byte* prawArray = imgdata)
                    ret = GetQHYCCDLiveFrame(handle, ref w, ref h, ref bpp, ref channels, prawArray);
                return ret;
            }

            [DllImport("qhyccd.dll", EntryPoint = "StopQHYCCDLive",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 StopQHYCCDLive(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDDebayerOnOff",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDDebayerOnOff(IntPtr handle,bool onoff);

            /************************************************************************************/
            /********************************* ReadMode Functions ************************************/
            /************************************************************************************/
            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDNumberOfReadModes",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDNumberOfReadModes(IntPtr handle, ref UInt32 numModes);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDReadModeResolution",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDReadModeResolution(IntPtr handle, UInt32 modeNumber, ref UInt32 width, ref UInt32 height);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDReadModeName",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDReadModeName(IntPtr handle, UInt32 modeNumber, byte[] name);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDReadMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDReadMode(IntPtr handle, UInt32 modeNumber);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDReadMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 GetQHYCCDReadMode(IntPtr handle, ref UInt32 modeNumber);

            /************************************************************************************/
            /********************************* GPS Functions ************************************/
            /************************************************************************************/
            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSVCOXFreq",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDGPSVCOXFreq(IntPtr handle, UInt32 i);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSLedCalMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDGPSLedCalMode(IntPtr handle, UInt32 i);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSLedCal",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern void SetQHYCCDGPSLedCal(IntPtr handle, UInt32 pos, UInt32 width);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSPOSA",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern void SetQHYCCDGPSPOSA(IntPtr handle, UInt32 is_slave, UInt32 pos, UInt32 width);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSPOSB",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern void SetQHYCCDGPSPOSB(IntPtr handle, UInt32 is_slave, UInt32 pos, UInt32 width);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSMasterSlave",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDGPSMasterSlave(IntPtr handle, UInt32 i);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDGPSSlaveModeParameter",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern void SetQHYCCDGPSSlaveModeParameter(IntPtr handle, UInt32 target_sec, UInt32 target_us, UInt32 deltaT_sec, UInt32 deltaT_us, UInt32 expTime);

            /**********************************************************************************/
            /******************************* Read and Write ***********************************/
            /**********************************************************************************/
            //[DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDLiveFrame",
            // CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            //public unsafe static extern UInt32 GetQHYCCDLiveFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte* imgdata);
            //public unsafe static UInt32 C_GetQHYCCDLiveFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte[] imgdata)
            //{
            //    UInt32 ret;
            //    fixed (byte* prawArray = imgdata)
            //        ret = GetQHYCCDLiveFrame(handle, ref w, ref h, ref bpp, ref channels, prawArray);
            //    return ret;
            //}

            [DllImport("qhyccd.dll", EntryPoint = "QHYCCDVendRequestWrite",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 QHYCCDVendRequestWrite(IntPtr handle, UInt32 req, UInt16 value, UInt16 index1, UInt32 length, byte *data);
            public unsafe static UInt32 C_QHYCCDVendRequestWrite(IntPtr handle, UInt32 req, UInt16 value, UInt16 index1, UInt32 length, byte[] data)
            {
                UInt32 ret;
                fixed (byte* pdata = data)
                    ret = QHYCCDVendRequestWrite(handle, req, value, index1, length, pdata);
                return ret;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="i"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "EnableQHYCCDImageOSD",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 EnableQHYCCDImageOSD(IntPtr handle, UInt32 i);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDFPGAVersion",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDFPGAVersion(IntPtr handle, UInt32 i, byte* verBuf);
            public unsafe static UInt32 C_GetQHYCCDFPGAVersion(IntPtr handle, UInt32 i, byte[] verBuf)
            {
                fixed (byte* pverBuf = verBuf)
                    return GetQHYCCDFPGAVersion(handle, i, pverBuf);
            }

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDReadingProgress",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern double GetQHYCCDReadingProgress(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDHumidity",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDHumidity(IntPtr handle, ref double hd);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDPressure",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDPressure(IntPtr handle, ref double pr);


            //EXPORTFUNC void RegisterPnpEventIn( void (*in_pnp_event_in_func)(char *id));
            //EXPORTFUNC void RegisterPnpEventOut( void (*in_pnp_event_out_func)(char *id));

            [DllImport("qhyccd.dll", EntryPoint = "RegisterPnpEventIn",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern void RegisterPnpEventIn(IntPtr callback);

            [DllImport("qhyccd.dll", EntryPoint = "RegisterPnpEventOut",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern void RegisterPnpEventOut(IntPtr callback);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDWriteCMOS",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDWriteCMOS(IntPtr handle, short number, UInt16 regindex, UInt16 regvalue);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDWriteFPGA",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDWriteFPGA(IntPtr handle, short number, short regindex, short regvalue);

            /*** Burst Mode ***/
            /// <summary>
            /// 
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="i"></param>
            /// <returns></returns>
            [DllImport("qhyccd.dll", EntryPoint = "EnableQHYCCDBurstMode",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 EnableQHYCCDBurstMode(IntPtr handle, bool i);
            //EXPORTC uint32_t STDCALL EnableQHYCCDBurstMode(qhyccd_handle *h,bool i);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBurstModeStartEnd",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDBurstModeStartEnd(IntPtr handle, ushort start, ushort end);
            //EXPORTC uint32_t STDCALL SetQHYCCDBurstModeStartEnd(qhyccd_handle *h,unsigned short start,unsigned short end);

            [DllImport("qhyccd.dll", EntryPoint = "EnableQHYCCDBurstCountFun",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 EnableQHYCCDBurstCountFun(IntPtr handle, bool i);
            //EXPORTC uint32_t STDCALL EnableQHYCCDBurstCountFun(qhyccd_handle *h,bool i);

            [DllImport("qhyccd.dll", EntryPoint = "ResetQHYCCDFrameCounter",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 ResetQHYCCDFrameCounter(IntPtr handle);
            //EXPORTC uint32_t STDCALL ResetQHYCCDFrameCounter(qhyccd_handle *h);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBurstIDLE",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDBurstIDLE(IntPtr handle);
            //EXPORTC uint32_t STDCALL SetQHYCCDBurstIDLE(qhyccd_handle *h);

            [DllImport("qhyccd.dll", EntryPoint = "ReleaseQHYCCDBurstIDLE",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 ReleaseQHYCCDBurstIDLE(IntPtr handle);
            //EXPORTC uint32_t STDCALL ReleaseQHYCCDBurstIDLE(qhyccd_handle *h);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBurstModePatchNumber",
             CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern Int32 SetQHYCCDBurstModePatchNumber(IntPtr handle, UInt32 value);
            //EXPORTC uint32_t STDCALL SetQHYCCDBurstModePatchNumber(qhyccd_handle *h,uint32_t value);
        }
    }


