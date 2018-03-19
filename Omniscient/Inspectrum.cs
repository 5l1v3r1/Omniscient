﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.WinForms;
using LiveCharts.Defaults;
using LiveCharts.Geared;

namespace Omniscient
{
    public partial class Inspectrum : Form
    {

        CHNParser chnParser;
        SPEParser speParser;
        GearedValues<ObservablePoint> chartVals;

        bool calibrationChanged = false;
        bool fileLoaded = false;

        double calibrationZero;
        double calibrationSlope;
        int[] counts;

        public Inspectrum()
        {
            calibrationZero = 0;
            calibrationSlope = 1;
            counts = new int[0];
            InitializeComponent();
            chnParser = new CHNParser();
            speParser = new SPEParser();
        }

        private void DrawSpectrum()
        {
            chartVals = new GearedValues<ObservablePoint>();
            List<ObservablePoint> list = new List<ObservablePoint>();
            for (int i = 0; i < counts.Length; ++i) //
            {
                list.Add(new ObservablePoint(calibrationZero + i * calibrationSlope, counts[i]));
            }
            chartVals = list.AsGearedValues().WithQuality(Quality.Highest);

            SpecChart.Series = new SeriesCollection()
            {
                new GStepLineSeries()
                {
                    Title = "Spectrum",
                    PointGeometry = null,
                    Values = chartVals
                }
            };

            if (SpecChart.AxisY.Count() == 0) SpecChart.AxisY.Add(new Axis() { MinValue = 0 });
            else SpecChart.AxisY[0].MinValue = 0;
            if (SpecChart.AxisX.Count() == 0) SpecChart.AxisX.Add(new Axis() { MinValue = 0 });
            else SpecChart.AxisX[0].MinValue = 0;
        }

        public void LoadSpectrumFile(string fileName)
        {
            Spectrum spectrum;
            string fileAbrev = fileName.Substring(fileName.LastIndexOf('\\') + 1);
            string fileExt = fileAbrev.Substring(fileAbrev.Length - 3).ToLower();
            if (fileExt == "chn")
            {
                chnParser.ParseSpectrumFile(fileName);
                spectrum = chnParser.GetSpectrum();
            }
            else if (fileExt == "spe")
            {
                speParser.ParseSpectrumFile(fileName);
                spectrum = speParser.GetSpectrum();
            }
            else
            {
                MessageBox.Show("Invalid file type!");
                return;
            }

            // Populate text fields
            FileNameTextBox.Text = fileName;
            DateTextBox.Text = spectrum.GetStartTime().ToString("dd-MMM-yyyy");
            TimeTextBox.Text = spectrum.GetStartTime().ToString("HH:mm:ss");
            CalZeroTextBox.Text = string.Format("{0:F3}", spectrum.GetCalibrationZero());
            CalSlopeTextBox.Text = string.Format("{0:F4}", spectrum.GetCalibrationSlope());

            LiveTimeTextBox.Text = string.Format("{0:F1} sec", spectrum.GetLiveTime());
            double deadTimePerc = 100 * (spectrum.GetRealTime() - spectrum.GetLiveTime()) / spectrum.GetRealTime();
            DeadTimeStripTextBox.Text = string.Format("{0:F2} %", deadTimePerc);

            calibrationZero = spectrum.GetCalibrationZero();
            calibrationSlope = spectrum.GetCalibrationSlope();
            counts = spectrum.GetCounts();
            DrawSpectrum();

            fileLoaded = true;
        }

        public void LoadCHNFile(string fileName)
        {
            if (chnParser.ParseSpectrumFile(fileName) == ReturnCode.SUCCESS)
            {
                // Populate text fields
                FileNameTextBox.Text = fileName;
                DateTextBox.Text = chnParser.GetStartDateTime().ToString("dd-MMM-yyyy");
                TimeTextBox.Text = chnParser.GetStartDateTime().ToString("HH:mm:ss");
                CalZeroTextBox.Text = string.Format("{0:F3}", chnParser.GetCalibrationZero());
                CalSlopeTextBox.Text = string.Format("{0:F4}", chnParser.GetCalibrationSlope());

                LiveTimeTextBox.Text = string.Format("{0:F1} sec", chnParser.GetLiveTime());
                double deadTimePerc = 100 * (chnParser.GetRealTime() - chnParser.GetLiveTime()) / chnParser.GetRealTime();
                DeadTimeStripTextBox.Text = string.Format("{0:F2} %", deadTimePerc);

                calibrationZero = chnParser.GetCalibrationZero();
                calibrationSlope = chnParser.GetCalibrationSlope();
                counts = chnParser.GetCounts();
                DrawSpectrum();

                fileLoaded = true;
            }
            else
            {
                MessageBox.Show("Error opening file!");
            }
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Spectrum Files|*.chn;*.spe|chn files (*.chn)|*.chn|spe files (*.spe)|*.spe|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadSpectrumFile(openFileDialog.FileName);
            }
        }

        private void OpenToolStripButton_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void Inspectrum_Load(object sender, EventArgs e)
        {

            SpecChart.DisableAnimations = true;
            SpecChart.Hoverable = false;
            SpecChart.DataTooltip = null;

            SeriesCollection seriesCollection = new SeriesCollection();
            SpecChart.Series = seriesCollection;
            SpecChart.Zoom = ZoomingOptions.X;
            if (fileLoaded) DrawSpectrum();
        }

        private void UpdateCalibration()
        {
            try
            {
                calibrationZero = double.Parse(CalZeroTextBox.Text);
                calibrationSlope = double.Parse(CalSlopeTextBox.Text);
                DrawSpectrum();
            }
            catch
            {
                MessageBox.Show("Invalid calibration!");
            }
        }

        private void CalZeroTextBox_TextChanged(object sender, EventArgs e)
        {
            calibrationChanged = true;
        }

        private void CalSlopeTextBox_TextChanged(object sender, EventArgs e)
        {
            calibrationChanged = true;
        }

        private void CalZeroTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (calibrationChanged & e.KeyCode == Keys.Enter)
            {
                UpdateCalibration();
            }
            calibrationChanged = false;
        }

        private void CalSlopeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (calibrationChanged & e.KeyCode == Keys.Enter)
            {
                UpdateCalibration();
            }
            calibrationChanged = false;
        }

        private void CalResetButton_Click(object sender, EventArgs e)
        {
            if(fileLoaded)
            {
                calibrationZero = chnParser.GetCalibrationZero();
                calibrationSlope = chnParser.GetCalibrationSlope();
                CalZeroTextBox.Text = string.Format("{0:F3}", chnParser.GetCalibrationZero());
                CalSlopeTextBox.Text = string.Format("{0:F4}", chnParser.GetCalibrationSlope());
                DrawSpectrum();
            }
        }
    }
}
