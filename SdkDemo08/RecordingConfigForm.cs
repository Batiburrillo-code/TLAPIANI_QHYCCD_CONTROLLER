using System;
using System.IO;
using System.Windows.Forms;

namespace SdkDemo08
{
    public partial class RecordingConfigForm : Form
    {
        // Propiedades públicas para acceder a los valores configurados
        public string DestinationName { get; private set; }
        public RecordingLimitType LimitType { get; private set; }
        public uint FrameCount { get; private set; }
        public TimeSpan TimeLimit { get; private set; }
        public bool UseSequence { get; private set; }
        public uint SequenceLength { get; private set; }
        public TimeSpan SequenceInterval { get; private set; }
        
        public bool DialogResultOK { get; private set; }

        // Controles
        private TextBox textBoxDestinationName;
        private RadioButton radioBtnUnlimited;
        private RadioButton radioBtnFrameCount;
        private RadioButton radioBtnTimeLimit;
        private NumericUpDown numericUpDownFrameCount;
        private DateTimePicker dateTimePickerTimeLimit;
        private CheckBox checkBoxSequence;
        private NumericUpDown numericUpDownSequenceLength;
        private DateTimePicker dateTimePickerSequenceInterval;
        private Button btnStart;
        private Button btnCancel;
        private Label labelDestinationName;
        private Label labelLimitType;
        private Label labelFrameCount;
        private Label labelTimeLimit;
        private Label labelSequence;
        private Label labelSequenceLength;
        private Label labelSequenceInterval;
        private GroupBox groupBoxLimit;
        private GroupBox groupBoxSequence;

        public RecordingConfigForm()
        {
            InitializeComponent();
            DialogResultOK = false;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuración básica del formulario
            this.Text = "SharpCap - Configurar captura";
            this.Size = new System.Drawing.Size(500, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = true;

            // Label y TextBox para Nombre de destino
            labelDestinationName = new Label();
            labelDestinationName.Text = "Nombre de destino:";
            labelDestinationName.Location = new System.Drawing.Point(12, 15);
            labelDestinationName.Size = new System.Drawing.Size(120, 20);
            labelDestinationName.AutoSize = false;
            this.Controls.Add(labelDestinationName);

            textBoxDestinationName = new TextBox();
            textBoxDestinationName.Location = new System.Drawing.Point(12, 35);
            textBoxDestinationName.Size = new System.Drawing.Size(460, 23);
            textBoxDestinationName.Text = "Recording";
            this.Controls.Add(textBoxDestinationName);

            // GroupBox para Seleccionar límite de captura
            groupBoxLimit = new GroupBox();
            groupBoxLimit.Text = "Seleccionar límite de captura";
            groupBoxLimit.Location = new System.Drawing.Point(12, 70);
            groupBoxLimit.Size = new System.Drawing.Size(460, 120);
            this.Controls.Add(groupBoxLimit);

            labelLimitType = new Label();
            labelLimitType.Text = "Esta sección controla la duración o cantidad total de imágenes que se van a capturar durante una sesión.";
            labelLimitType.Location = new System.Drawing.Point(10, 20);
            labelLimitType.Size = new System.Drawing.Size(440, 30);
            labelLimitType.AutoSize = false;
            groupBoxLimit.Controls.Add(labelLimitType);

            // Radio button Ilimitado
            radioBtnUnlimited = new RadioButton();
            radioBtnUnlimited.Text = "Ilimitado";
            radioBtnUnlimited.Location = new System.Drawing.Point(10, 50);
            radioBtnUnlimited.Size = new System.Drawing.Size(100, 20);
            radioBtnUnlimited.Checked = true;
            radioBtnUnlimited.CheckedChanged += RadioBtnUnlimited_CheckedChanged;
            groupBoxLimit.Controls.Add(radioBtnUnlimited);

            // Radio button Número de cuadros
            radioBtnFrameCount = new RadioButton();
            radioBtnFrameCount.Text = "Número de cuadros:";
            radioBtnFrameCount.Location = new System.Drawing.Point(10, 75);
            radioBtnFrameCount.Size = new System.Drawing.Size(130, 20);
            radioBtnFrameCount.CheckedChanged += RadioBtnFrameCount_CheckedChanged;
            groupBoxLimit.Controls.Add(radioBtnFrameCount);

            numericUpDownFrameCount = new NumericUpDown();
            numericUpDownFrameCount.Location = new System.Drawing.Point(145, 73);
            numericUpDownFrameCount.Size = new System.Drawing.Size(80, 23);
            numericUpDownFrameCount.Minimum = 1;
            numericUpDownFrameCount.Maximum = 999999;
            numericUpDownFrameCount.Value = 100;
            numericUpDownFrameCount.Enabled = false;
            groupBoxLimit.Controls.Add(numericUpDownFrameCount);

            // Radio button Límite de tiempo
            radioBtnTimeLimit = new RadioButton();
            radioBtnTimeLimit.Text = "Límite de tiempo:";
            radioBtnTimeLimit.Location = new System.Drawing.Point(10, 100);
            radioBtnTimeLimit.Size = new System.Drawing.Size(130, 20);
            radioBtnTimeLimit.CheckedChanged += RadioBtnTimeLimit_CheckedChanged;
            groupBoxLimit.Controls.Add(radioBtnTimeLimit);

            // DateTimePicker para tiempo (solo tiempo, no fecha)
            dateTimePickerTimeLimit = new DateTimePicker();
            dateTimePickerTimeLimit.Location = new System.Drawing.Point(145, 98);
            dateTimePickerTimeLimit.Size = new System.Drawing.Size(120, 23);
            dateTimePickerTimeLimit.Format = DateTimePickerFormat.Custom;
            dateTimePickerTimeLimit.CustomFormat = "HH:mm:ss";
            dateTimePickerTimeLimit.ShowUpDown = true;
            dateTimePickerTimeLimit.Value = new DateTime(2000, 1, 1, 0, 1, 0); // 00:01:00
            dateTimePickerTimeLimit.Enabled = false;
            groupBoxLimit.Controls.Add(dateTimePickerTimeLimit);

            // GroupBox para Secuencia simple
            groupBoxSequence = new GroupBox();
            groupBoxSequence.Text = "Secuencia simple";
            groupBoxSequence.Location = new System.Drawing.Point(12, 200);
            groupBoxSequence.Size = new System.Drawing.Size(460, 120);
            this.Controls.Add(groupBoxSequence);

            // CheckBox para realizar secuencia
            checkBoxSequence = new CheckBox();
            checkBoxSequence.Text = "Realizar una secuencia de capturas";
            checkBoxSequence.Location = new System.Drawing.Point(10, 20);
            checkBoxSequence.Size = new System.Drawing.Size(200, 20);
            checkBoxSequence.CheckedChanged += CheckBoxSequence_CheckedChanged;
            groupBoxSequence.Controls.Add(checkBoxSequence);

            // Label y NumericUpDown para Longitud de secuencia
            labelSequenceLength = new Label();
            labelSequenceLength.Text = "Longitud de la secuencia:";
            labelSequenceLength.Location = new System.Drawing.Point(10, 50);
            labelSequenceLength.Size = new System.Drawing.Size(150, 20);
            groupBoxSequence.Controls.Add(labelSequenceLength);

            numericUpDownSequenceLength = new NumericUpDown();
            numericUpDownSequenceLength.Location = new System.Drawing.Point(165, 48);
            numericUpDownSequenceLength.Size = new System.Drawing.Size(80, 23);
            numericUpDownSequenceLength.Minimum = 1;
            numericUpDownSequenceLength.Maximum = 9999;
            numericUpDownSequenceLength.Value = 1;
            numericUpDownSequenceLength.Enabled = false;
            groupBoxSequence.Controls.Add(numericUpDownSequenceLength);

            // Label y DateTimePicker para Intervalo entre capturas
            labelSequenceInterval = new Label();
            labelSequenceInterval.Text = "Intervalo entre capturas (HH:MM:SS):";
            labelSequenceInterval.Location = new System.Drawing.Point(10, 80);
            labelSequenceInterval.Size = new System.Drawing.Size(200, 20);
            groupBoxSequence.Controls.Add(labelSequenceInterval);

            dateTimePickerSequenceInterval = new DateTimePicker();
            dateTimePickerSequenceInterval.Location = new System.Drawing.Point(215, 78);
            dateTimePickerSequenceInterval.Size = new System.Drawing.Size(120, 23);
            dateTimePickerSequenceInterval.Format = DateTimePickerFormat.Custom;
            dateTimePickerSequenceInterval.CustomFormat = "HH:mm:ss";
            dateTimePickerSequenceInterval.ShowUpDown = true;
            dateTimePickerSequenceInterval.Value = new DateTime(2000, 1, 1, 0, 5, 0); // 00:05:00
            dateTimePickerSequenceInterval.Enabled = false;
            groupBoxSequence.Controls.Add(dateTimePickerSequenceInterval);

            // Botones
            btnStart = new Button();
            btnStart.Text = "Comienzo";
            btnStart.Location = new System.Drawing.Point(300, 330);
            btnStart.Size = new System.Drawing.Size(80, 30);
            btnStart.DialogResult = DialogResult.OK;
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);
            this.AcceptButton = btnStart;

            btnCancel = new Button();
            btnCancel.Text = "Cancelar";
            btnCancel.Location = new System.Drawing.Point(390, 330);
            btnCancel.Size = new System.Drawing.Size(80, 30);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;

            this.ResumeLayout(false);
        }

        private void RadioBtnUnlimited_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnUnlimited.Checked)
            {
                numericUpDownFrameCount.Enabled = false;
                dateTimePickerTimeLimit.Enabled = false;
            }
        }

        private void RadioBtnFrameCount_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnFrameCount.Checked)
            {
                numericUpDownFrameCount.Enabled = true;
                dateTimePickerTimeLimit.Enabled = false;
            }
        }

        private void RadioBtnTimeLimit_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtnTimeLimit.Checked)
            {
                numericUpDownFrameCount.Enabled = false;
                dateTimePickerTimeLimit.Enabled = true;
            }
        }

        private void CheckBoxSequence_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = checkBoxSequence.Checked;
            numericUpDownSequenceLength.Enabled = enabled;
            dateTimePickerSequenceInterval.Enabled = enabled;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(textBoxDestinationName.Text))
            {
                MessageBox.Show("Por favor ingrese un nombre de destino.", "Error de validación", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxDestinationName.Focus();
                return;
            }

            // Obtener valores
            DestinationName = textBoxDestinationName.Text.Trim();

            if (radioBtnUnlimited.Checked)
                LimitType = RecordingLimitType.Unlimited;
            else if (radioBtnFrameCount.Checked)
                LimitType = RecordingLimitType.FrameCount;
            else if (radioBtnTimeLimit.Checked)
                LimitType = RecordingLimitType.TimeLimit;

            FrameCount = (uint)numericUpDownFrameCount.Value;
            TimeLimit = dateTimePickerTimeLimit.Value.TimeOfDay;
            UseSequence = checkBoxSequence.Checked;
            SequenceLength = (uint)numericUpDownSequenceLength.Value;
            SequenceInterval = dateTimePickerSequenceInterval.Value.TimeOfDay;

            DialogResultOK = true;
            this.DialogResult = DialogResult.OK;
        }
    }

    public enum RecordingLimitType
    {
        Unlimited,
        FrameCount,
        TimeLimit
    }
}

