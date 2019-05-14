﻿// This software is open source software available under the BSD-3 license.
// 
// Copyright (c) 2018, Los Alamos National Security, LLC
// All rights reserved.
// 
// Copyright 2018. Los Alamos National Security, LLC. This software was produced under U.S. Government contract DE-AC52-06NA25396 for Los Alamos National Laboratory (LANL), which is operated by Los Alamos National Security, LLC for the U.S. Department of Energy. The U.S. Government has rights to use, reproduce, and distribute this software.  NEITHER THE GOVERNMENT NOR LOS ALAMOS NATIONAL SECURITY, LLC MAKES ANY WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS SOFTWARE.  If software is modified to produce derivative works, such modified software should be clearly marked, so as not to confuse it with the version available from LANL.
// 
// Additionally, redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 1.       Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// 2.       Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
// 3.       Neither the name of Los Alamos National Security, LLC, Los Alamos National Laboratory, LANL, the U.S. Government, nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission. 
//  
// THIS SOFTWARE IS PROVIDED BY LOS ALAMOS NATIONAL SECURITY, LLC AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL LOS ALAMOS NATIONAL SECURITY, LLC OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

using System.Windows.Media;

using Omniscient.MainDialogs;

namespace Omniscient
{
    /// <summary>
    /// MainForm is the window that appears when opening Omniscient as is where most of the user-interaction occurs.
    /// </summary>
    public partial class MainForm : Form
    {
        ///////////////////////////////////////////////////////////////////////
        public OmniscientCore Core;

        private const int N_CHARTS = 4;
        private const int MAX_HIGHLIGHTED_EVENTS = 60;

        public PresetManager presetMan;
        List<ChannelPanel> chPanels;

        private bool rangeChanged = false;
        private bool bootingUp = false;

        private bool[] logScale;
        private bool[] autoScale;
        double[] chartMaxPointValue;
        double[] chartMinPointValue;

        double[] chartMinY;
        double[] chartMaxY;

        // The following are used to show a time marker when a user clicks a chart
        Chart activeChart;
        double mouseX = 0;
        double mouseY = 0;
        double mouseDownX = 0;
        double mouseDownY = 0;
        double mouseUpX = 0;
        DateTime mouseTime;
        private bool showMarker = false;
        private double markerValue = 0;
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// MainForm constructor
        /// </summary>
        public MainForm()
        {
            Core = new OmniscientCore();
            if (Core.ErrorMessage != "")
            {
                MessageBox.Show(Core.ErrorMessage);
            }
            logScale = new bool[N_CHARTS];
            autoScale = new bool[N_CHARTS];
            for (int c = 0; c < N_CHARTS; c++)
            {
                logScale[c] = false;
                autoScale[c] = true;
            }
            InitializeComponent();
            Core.ViewChanged += Core_OnViewChanged;
        }

        private void Core_OnViewChanged(object sender, EventArgs e)
        {
            StartDatePicker.Value = Core.ViewStart.Date;
            StartTimePicker.Value = Core.ViewStart;
            EndDatePicker.Value = Core.ViewEnd.Date;
            EndTimePicker.Value = Core.ViewEnd;

            long range = (Core.ViewEnd - Core.ViewStart).Ticks;

            if (range % TimeSpan.FromDays(1).Ticks == 0)
            {
                RangeComboBox.Text = "Days";
                RangeTextBox.Text = (range / TimeSpan.FromDays(1).Ticks).ToString();
            }
            else if (range % TimeSpan.FromHours(1).Ticks == 0)
            {
                RangeComboBox.Text = "Hours";
                RangeTextBox.Text = (range / TimeSpan.FromHours(1).Ticks).ToString();
            }
            else if (range % TimeSpan.FromMinutes(1).Ticks == 0)
            {
                RangeComboBox.Text = "Minutes";
                RangeTextBox.Text = (range / TimeSpan.FromMinutes(1).Ticks).ToString();
            }
            else
            {
                RangeComboBox.Text = "Seconds";
                RangeTextBox.Text = (range / TimeSpan.FromSeconds(1).Ticks).ToString();
            }

            UpdateRange();
        }

        /// <summary>
        /// MainForm_Load initializes several variables after the form is constructed.</summary>
        /// <remarks>Event hanlders for mouse events on the charts are handled 
        /// here because they cause errors when put in the form designer. </remarks>
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = "Omniscient - Version " + OmniscientCore.VERSION;
            bootingUp = true;
            StripChart0.MouseDown += new MouseEventHandler(StripChart_MouseClick);
            StripChart1.MouseDown += new MouseEventHandler(StripChart_MouseClick);
            StripChart2.MouseDown += new MouseEventHandler(StripChart_MouseClick);
            StripChart3.MouseDown += new MouseEventHandler(StripChart_MouseClick);
            StripChart0.MouseMove += new MouseEventHandler(StripChart_MouseMoved);
            StripChart1.MouseMove += new MouseEventHandler(StripChart_MouseMoved);
            StripChart2.MouseMove += new MouseEventHandler(StripChart_MouseMoved);
            StripChart3.MouseMove += new MouseEventHandler(StripChart_MouseMoved);
            StripChart0.MouseWheel += StripChart_MouseWheel;
            StripChart1.MouseWheel += StripChart_MouseWheel;
            StripChart2.MouseWheel += StripChart_MouseWheel;
            StripChart3.MouseWheel += StripChart_MouseWheel;
            StripChart0.Paint += new PaintEventHandler(StripChart_Paint);
            StripChart1.Paint += new PaintEventHandler(StripChart_Paint);
            StripChart2.Paint += new PaintEventHandler(StripChart_Paint);
            StripChart3.Paint += new PaintEventHandler(StripChart_Paint);

            StripChart0.SuppressExceptions = true;
            StripChart1.SuppressExceptions = true;
            StripChart2.SuppressExceptions = true;
            StripChart3.SuppressExceptions = true;

            GlobalStartTextBox.Text = Core.GlobalStart.ToString("MMM dd, yyyy");
            GlobalEndTextBox.Text = Core.GlobalEnd.ToString("MMM dd, yyyy");
            StartDatePicker.Value = Core.ViewStart;
            EndDatePicker.Value = Core.ViewEnd;
            StartTimePicker.Value = Core.ViewStart.Date;
            EndTimePicker.Value = Core.ViewEnd.Date;
            chPanels = new List<ChannelPanel>();
            
            LoadPresets();
            UpdateSitesTree();
            RangeTextBox.Text = "1";
            RangeComboBox.Text = "Days";
            InitializeCharts();
            bootingUp = false;
            UpdateRange();
        }

        private void StripChart_MouseWheel(object sender, MouseEventArgs e)
        {
            // Vertical zoom
            Chart chart = (Chart)sender;
            int chartNum = (int)chart.Tag;
            if (e.Delta < 0)
            {
                if (chart.ChartAreas[0].AxisY.Maximum < 1e6 * chartMaxPointValue[chartNum])
                    chart.ChartAreas[0].AxisY.Maximum *= 1.1;
            }
            else
            {
                if (chart.ChartAreas[0].AxisY.Maximum > 1e-6 * chartMaxPointValue[chartNum])
                    chart.ChartAreas[0].AxisY.Maximum *= 0.9;
            }
        }

        public void CheckFullNodes()
        {
            foreach (TreeNode node in SitesTreeView.Nodes)
                CheckFullNodes(node);
        }

        public void CheckFullNodes(TreeNode topNode)
        {
            if (topNode.Nodes.Count == 0) return;

            bool allChecked = true;
            foreach (TreeNode child in topNode.Nodes)
            {
                CheckFullNodes(child);
                if (!child.Checked) allChecked = false;
            }
            if (allChecked && (!topNode.Checked))
                topNode.Checked = true;
        }

        public void CheckChildrenNodes(TreeNode node)
        {
            if (!node.Checked)
                node.Checked = true;
            foreach (TreeNode child in node.Nodes)
            {
                CheckChildrenNodes(child);
            }
        }

        public void UncheckChildrenNodes(TreeNode node)
        {
            if (node.Checked)
                node.Checked = false;
            foreach (TreeNode child in node.Nodes)
            {
                UncheckChildrenNodes(child);
            }
        }

        public void ClearPanels()
        {
            foreach (TreeNode child in SitesTreeView.Nodes)
            {
                UncheckChildrenNodes(child);
            }

            while (Core.ActiveInstruments.Count>0)
            {
                //SitesTreeView.Nodes.Find(activeInstruments[0].GetName(), true)[0].Checked = false;
                RemoveChannelPanels(Core.ActiveInstruments[0]);
            }
        }
        
        public void LoadPresets()
        {
            presetMan = new PresetManager("Presets.xml", Core.SiteManager);
            ReturnCode returnCode = presetMan.Reload();
            if (returnCode == ReturnCode.FILE_DOESNT_EXIST)
            {
                presetMan.WriteBlank();
                MessageBox.Show("Presets.xml not found. A new one has been created.");
            }
            else if (returnCode != ReturnCode.SUCCESS) MessageBox.Show("Warning: Bad trouble loading the preset manager!");
            PresetsComboBox.Items.Clear();
            foreach (Preset preset in presetMan.GetPresets())
            {
                PresetsComboBox.Items.Add(preset.GetName());
            }
        }

        /// <summary>
        /// UpdateSitesTree() builds the tree view of the SiteManager.</summary>
        public void UpdateSitesTree()
        {
            SitesTreeView.Nodes.Clear();
            foreach (Site site in Core.SiteManager.GetSites())
            {
                TreeNode siteNode = new TreeNode(site.Name);
                siteNode.Name = site.Name;
                siteNode.Tag = site;
                siteNode.ImageIndex = 0;
                siteNode.SelectedImageIndex = 0;
                siteNode.ToolTipText = siteNode.Text;
                foreach (Facility fac in site.GetFacilities())
                {
                    TreeNode facNode = new TreeNode(fac.Name);
                    facNode.Name = fac.Name;
                    facNode.Tag = fac;
                    facNode.ImageIndex = 1;
                    facNode.SelectedImageIndex = 1;
                    facNode.ToolTipText = facNode.Text;
                    foreach (DetectionSystem sys in fac.GetSystems())
                    {
                        TreeNode sysNode = new TreeNode(sys.Name);
                        sysNode.Name = sys.Name;
                        sysNode.Tag = sys;
                        sysNode.ImageIndex = 2;
                        sysNode.SelectedImageIndex = 2;
                        sysNode.ToolTipText = sysNode.Text;
                        foreach (Instrument inst in sys.GetInstruments())
                        {
                            TreeNode instNode = new TreeNode(inst.Name);
                            instNode.Name = inst.Name;
                            instNode.Tag = inst;
                            instNode.ImageIndex = 3;
                            instNode.SelectedImageIndex = 3;
                            instNode.ToolTipText = instNode.Text;
                            sysNode.Nodes.Add(instNode);
                        }
                        foreach (EventGenerator eg in sys.GetEventGenerators())
                        {
                            TreeNode egNode = new TreeNode(eg.Name);
                            egNode.Name = eg.Name;
                            egNode.Tag = eg;
                            egNode.ImageIndex = 4;
                            egNode.SelectedImageIndex = 4;
                            egNode.ToolTipText = egNode.Text;
                            sysNode.Nodes.Add(egNode);
                        }
                        facNode.Nodes.Add(sysNode);
                    }
                    facNode.Expand();
                    siteNode.Nodes.Add(facNode);
                }
                siteNode.Expand();
                SitesTreeView.Nodes.Add(siteNode);
            }
        }

        /// <summary>
        /// GetChart is used to easily loop through the charts in other methods.</summary>
        private Chart GetChart(int chartNum)
        {
            switch (chartNum)
            {
                case 0:
                    return StripChart0;
                case 1:
                    return StripChart1;
                case 2:
                    return StripChart2;
                default:
                    return StripChart3;
            }
        }

        private void launchInspectrumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Inspectrum inspectrum = new Inspectrum();
            inspectrum.Show();
        }

        /// <summary>
        /// InitializeCharts is called when the form is loaded. </summary>
        private void InitializeCharts()
        {
            chartMinY = new double[N_CHARTS];
            chartMaxY = new double[N_CHARTS];
            chartMaxPointValue = new double[N_CHARTS];
            chartMinPointValue = new double[N_CHARTS];
            for (int chartNum = 0; chartNum < N_CHARTS; chartNum++)
            {
                Chart chart;
                chart = GetChart(chartNum);
                chart.AntiAliasing = AntiAliasingStyles.All;
                chart.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
                chart.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
                chart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
                chart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
                chart.Tag = chartNum;
                chart.GetToolTipText += new EventHandler<ToolTipEventArgs>(GetChartToolTip);
                // Initizialize chart values
                /*
                chart.AxisX.Clear();
                chart.AxisY.Clear();
                chart.AxisX.Add(new Axis
                {
                    LabelFormatter = val => new System.DateTime((long)val).ToString("MM DD YYY"),
                    MinValue = DateTime.Today.Ticks,
                    MaxValue = DateTime.Today.Ticks + TimeSpan.TicksPerDay,
                    //Separator = sep
                });
                */
                Series series = chart.Series[0];
                
                series.Points.Clear();
                chartMinY[chartNum] = 0;
                chartMaxY[chartNum] = 0;
                chartMaxPointValue[chartNum] = 0;
                chartMinPointValue[chartNum] = 0;
            }
        }

        private void GetChartToolTip(object sender, ToolTipEventArgs e)
        {
            Chart chart = (Chart)sender;
           
            double distSq = 1e9;
            double thisDistSq;
            double yVal = 0;
            string sName = "";
            foreach(Series series in chart.Series)
            {
                foreach(DataPoint point in series.Points)
                {
                    double deltaX = Math.Abs(e.X - chart.ChartAreas[0].AxisX.ValueToPixelPosition(point.XValue));
                    double deltaY = Math.Abs(e.Y - chart.ChartAreas[0].AxisY.ValueToPixelPosition(point.YValues[0]));
                    thisDistSq = deltaX * deltaX + deltaY * deltaY;
                    if (thisDistSq < distSq)
                    {
                        distSq = thisDistSq;
                        yVal = point.YValues[0];
                        sName = series.Name;
                    }
                }
            }
            if (distSq < 100)
                e.Text = (sName + "\n" + yVal.ToString());
        }

        /// <summary>
        /// UpdateChart is called whenever the data to be displayed on a chart changes. </summary>
        private void UpdateChart(int chartNum)
        {
            const double MAX_VALUE = (double)decimal.MaxValue;
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            Chart chart;
            chart = GetChart(chartNum);
            chart.SuspendLayout();

            // Needed for speedy loading
            DateTime start = Core.ViewStart;
              DateTime end = Core.ViewEnd;

            if (logScale[chartNum])
                chart.ChartAreas[0].AxisY.IsLogarithmic = true;
            else
                chart.ChartAreas[0].AxisY.IsLogarithmic = false;
            chart.Series.Clear();
            chartMaxPointValue[chartNum] = double.MinValue;
            chartMinPointValue[chartNum] = double.MaxValue;
            foreach (ChannelPanel chanPan in chPanels)
            {
                bool plotChan = false;
                switch (chartNum)
                {
                    case 0:
                        if (chanPan.Chart1CheckBox.Checked)
                            plotChan = true;
                        break;
                    case 1:
                        if (chanPan.Chart2CheckBox.Checked)
                            plotChan = true;
                        break;
                    case 2:
                        if (chanPan.Chart3CheckBox.Checked)
                            plotChan = true;
                        break;
                    case 3:
                        if (chanPan.Chart4CheckBox.Checked)
                            plotChan = true;
                        break;
                }
                if (plotChan)
                {
                    Channel chan = chanPan.GetChannel();

                    List<DateTime> dates = chan.GetTimeStamps(ChannelCompartment.View);
                    List<double> vals = chan.GetValues(ChannelCompartment.View);

                    Series series = new Series(chan.Name);
                    series.Points.SuspendUpdates();
                    series.ChartType = SeriesChartType.FastLine;
                    series.XValueType = ChartValueType.DateTime;
                    
                    if (chan.GetChannelType() == Channel.ChannelType.DURATION_VALUE)
                    {
                        List<TimeSpan> durations = chan.GetDurations(ChannelCompartment.View);
                        if (logScale[chartNum])
                        {
                            for (int i = 0; i < dates.Count; ++i)
                            {
                                if (!(double.IsNaN(vals[i])) && vals[i] < MAX_VALUE && vals[i] > 0 && dates[i] + durations[i] >= start && dates[i] <= end)
                                {
                                    series.Points.AddXY(dates[i].ToOADate(), vals[i]);
                                    series.Points.AddXY((dates[i] + durations[i]).ToOADate(), vals[i]);
                                    series.Points.AddXY((dates[i] + durations[i].Add(TimeSpan.FromTicks(1))).ToOADate(), double.NaN);
                                    if (vals[i] > chartMaxPointValue[chartNum]) chartMaxPointValue[chartNum] = vals[i];
                                    if (vals[i] < chartMinPointValue[chartNum]) chartMinPointValue[chartNum] = vals[i];
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (!(double.IsNaN(vals[i])) && vals[i] < MAX_VALUE && dates[i] + durations[i] >= start && dates[i] <= end)
                                {
                                    series.Points.AddXY(dates[i].ToOADate(), vals[i]);
                                    series.Points.AddXY((dates[i] + durations[i]).ToOADate(), vals[i]);
                                    series.Points.AddXY((dates[i] + durations[i].Add(TimeSpan.FromTicks(1))).ToOADate(), double.NaN);
                                    if (vals[i] > chartMaxPointValue[chartNum]) chartMaxPointValue[chartNum] = vals[i];
                                    if (vals[i] < chartMinPointValue[chartNum]) chartMinPointValue[chartNum] = vals[i];
                                }
                            }
                        }
                    }
                    else
                    {
                        // Load up the chart values
                        DataDecimator dataDecimator = new DataDecimator();
                        dataDecimator.TimeStamps = dates;
                        dataDecimator.Values = vals;
                        dataDecimator.Decimate(1000, start, end);
                        dates = dataDecimator.TimeStamps;
                        vals = dataDecimator.Values;

                        if (logScale[chartNum])
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (!(double.IsNaN(vals[i])) && vals[i] > 0 && vals[i] < MAX_VALUE && dates[i] >= start && dates[i] <= end)
                                {
                                    series.Points.AddXY(dates[i].ToOADate(), vals[i]);
                                    if (vals[i] > chartMaxPointValue[chartNum]) chartMaxPointValue[chartNum] = vals[i];
                                    if (vals[i] < chartMinPointValue[chartNum]) chartMinPointValue[chartNum] = vals[i];
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (!(double.IsNaN(vals[i])) && vals[i] < MAX_VALUE && dates[i] >= start && dates[i] <= end)
                                {
                                    series.Points.AddXY(dates[i].ToOADate(), vals[i]);
                                    if (vals[i] > chartMaxPointValue[chartNum]) chartMaxPointValue[chartNum] = vals[i];
                                    if (vals[i] < chartMinPointValue[chartNum]) chartMinPointValue[chartNum] = vals[i];
                                }
                            }
                        }
                    }
                    chart.Series.Add(series);
                    series.Points.ResumeUpdates();
                }
            }
            if(autoScale[chartNum]) AutoScaleYAxes(chartNum);
            chart.ChartAreas[0].AxisY.Minimum = chartMinY[chartNum];
            chart.ChartAreas[0].AxisY.Maximum = chartMaxY[chartNum];
            chart.ResumeLayout();
            System.Windows.Forms.Cursor.Current = Cursors.Default;
        }

        private void AutoScaleYAxes(int chartNum)
        {
            double maxOrderOfMagnitude = Math.Pow(10,Math.Floor(Math.Log10(chartMaxPointValue[chartNum])));
            double firstDigit = Math.Floor(chartMaxPointValue[chartNum] / maxOrderOfMagnitude);
            double maxMinRatio = chartMaxPointValue[chartNum] / chartMinPointValue[chartNum];

            if(logScale[chartNum])
            {
                double minOrderOfMagnitude = Math.Pow(10, Math.Floor(Math.Log10(chartMinPointValue[chartNum])));
                chartMinY[chartNum] = minOrderOfMagnitude;
                chartMaxY[chartNum] = maxOrderOfMagnitude * 10;
            }
            else if(maxMinRatio > 2)
            {
                chartMinY[chartNum] = 0;
                chartMaxY[chartNum] = (firstDigit+1)*maxOrderOfMagnitude;
            }
            else
            {
                double minOrderOfMagnitude = Math.Pow(10,Math.Floor(Math.Log10(chartMinPointValue[chartNum])));
                double maxMinDifference = chartMaxPointValue[chartNum] - chartMinPointValue[chartNum];
                double diffOoM = Math.Pow(10, Math.Floor(Math.Log10(maxMinDifference)));

                chartMinY[chartNum] = Math.Floor(chartMinPointValue[chartNum] / (diffOoM)) * diffOoM;
                chartMaxY[chartNum] = Math.Ceiling(chartMaxPointValue[chartNum] / (diffOoM)) * diffOoM;
            }
        }

        private void DrawSections()
        {
            DateTime start = Core.ViewStart;
            DateTime end = Core.ViewEnd;
            int eventCount = 0;
            Chart chart;
            bool overHeightWarning = false;
            for (int chartNum = 0; chartNum < N_CHARTS; chartNum++)
            {               
                chart = GetChart(chartNum);
                chart.Annotations.Clear();
                chart.Update();

                if (markerValue > start.ToOADate() && markerValue < end.ToOADate())
                {
                    VerticalLineAnnotation line = new VerticalLineAnnotation();
                    line.AxisX = chart.ChartAreas[0].AxisX;
                    line.IsInfinitive = true;
                    line.X = markerValue;
                    line.LineColor = System.Drawing.Color.FromArgb(64, 64, 64);
                    line.Width = 1;
                    chart.Annotations.Add(line);
                }
                eventCount = 0;
                if (HighlightEventsCheckBox.Checked)
                {
                    foreach (Event eve in Core.Events)
                    {
                        if (eve.GetStartTime() < end && eve.GetEndTime() > start && eventCount < MAX_HIGHLIGHTED_EVENTS)
                        {
                            double width = eve.GetEndTime().ToOADate() - eve.GetStartTime().ToOADate();
                            RectangleAnnotation rect = new RectangleAnnotation();
                            rect.AxisX = chart.ChartAreas[0].AxisX;
                            rect.AxisY = chart.ChartAreas[0].AxisY;
                            rect.AnchorX = eve.GetStartTime().ToOADate() + width/2;
                            rect.AnchorY = rect.AxisY.Minimum;
                            rect.IsSizeAlwaysRelative = false;
                            rect.LineWidth = 0;

                            rect.BackColor = System.Drawing.Color.FromArgb(128, 0, 255, 0);
                            if (eve.GetEventGenerator() is CoincidenceEG)
                            {
                                rect.BackColor = System.Drawing.Color.FromArgb(128, 243, 243, 21);
                                rect.BackHatchStyle = ChartHatchStyle.WideUpwardDiagonal;
                            }
                            //rect.Width = (eve.GetEndTime().Ticks - eve.GetStartTime().Ticks > 0) ? 
                            //    rect.AxisX.ValueToPixelPosition(eve.GetEndTime().ToOADate()) - rect.AxisX.ValueToPixelPosition(eve.GetStartTime().ToOADate()) :
                            //    rect.AxisX.ValueToPixelPosition((eve.GetStartTime() + TimeSpan.FromSeconds(5)).ToOADate()) - rect.AxisX.ValueToPixelPosition(eve.GetStartTime().ToOADate());
                            rect.Width = eve.GetEndTime().ToOADate() - eve.GetStartTime().ToOADate();
                            double tempHeight = rect.AxisY.Maximum - rect.AxisY.Minimum;
                            if (tempHeight < 290000000) rect.Height = rect.AxisY.Maximum - rect.AxisY.Minimum;
                            else if(!double.IsNaN(tempHeight))
                            {
                                rect.Height = 290000000 - 1;
                                overHeightWarning = true;
                            }
                            chart.Annotations.Add(rect);
                            eventCount++;
                        }
                    }
                }
            }
            if (overHeightWarning)
            {
                EventsWarningLabel.Text = "Warning: Y-Axis range may be too large to properly display highlighted events!";
            }
            else if (eventCount == MAX_HIGHLIGHTED_EVENTS)
                EventsWarningLabel.Text = "Warning: Too many events in view. Not all are highlighted";
            else
                EventsWarningLabel.Text = "";
        }

        /// <summary>
        /// UpdateChartVisibility is called whenever the user clicks on a 
        /// checkbox in a channel panel.</summary>
        /// <remarks>Determines which charts have data to be displayed and 
        /// adjusts the size of the table layout panel accordingly.</remarks>
        private void UpdateChartVisibility()
        {
            StripChartsLayoutPanel.SuspendLayout();
            int numVisible = 0;
            for (int chartNum=0; chartNum< N_CHARTS; chartNum++)
            {
                Chart chart = GetChart(chartNum);
                if (chart.Series.Count > 0) numVisible++;
            }
            for (int chartNum = 0; chartNum < N_CHARTS; chartNum++)
            {
                Chart chart = GetChart(chartNum);
                if (chart.Series.Count > 0)
                {
                    StripChartsLayoutPanel.RowStyles[chartNum].SizeType = SizeType.Percent;
                    StripChartsLayoutPanel.RowStyles[chartNum].Height = (float)100.0 / numVisible;
                }
                else
                {
                    StripChartsLayoutPanel.RowStyles[chartNum].Height = 0;
                    chart.ChartAreas[0].AxisY.Maximum = 1;
                    chart.ChartAreas[0].AxisY.Minimum = 0;
                }
            }
            StripChartsLayoutPanel.ResumeLayout();
        }

        /// <summary>
        /// OnChannelPannelCheckChanged is directly called whenever the user 
        /// clicks on a checkbox in a channel panel.</summary>
        private void OnChannelPannelCheckChanged(object sender, EventArgs e)
        {
            CheckBox checker = (CheckBox)sender;
            UpdateChart((int)checker.Tag);
            UpdateChartVisibility();
        }

        /// <summary>
        /// UpdateGlobalStartEnd is called whenever a channel panel is either
        /// added or removed.</summary>
        /// <remarks>It updates globalStart and globalEnd and changes the
        /// start/end pickers if they are out of the global range.</remarks>
        private void UpdateGlobalStartEnd()
        {
            // Update global start and end
            GlobalStartTextBox.Text = Core.GlobalStart.ToString("MMM dd, yyyy");
            GlobalEndTextBox.Text = Core.GlobalEnd.ToString("MMM dd, yyyy");

            if (StartDatePicker.Value.Date.Add(StartTimePicker.Value.TimeOfDay) != Core.ViewStart ||
                EndDatePicker.Value.Date.Add(EndTimePicker.Value.TimeOfDay) != Core.ViewEnd)
            {
                if (TimeSpan.FromTicks(Core.ViewEnd.Ticks - Core.ViewStart.Ticks).TotalDays > 1)
                {
                    RangeTextBox.Text = "1";
                    RangeComboBox.Text = "Days";
                }
                else if (TimeSpan.FromTicks(Core.ViewEnd.Ticks - Core.ViewStart.Ticks).TotalDays > 1)
                {
                    RangeTextBox.Text = "24";
                    RangeComboBox.Text = "Hours";
                }
                else if (TimeSpan.FromTicks(Core.ViewEnd.Ticks - Core.ViewStart.Ticks).TotalHours > 1)
                {
                    RangeTextBox.Text = "60";
                    RangeComboBox.Text = "Minutes";
                }
                else
                {
                    RangeTextBox.Text = "";
                    RangeComboBox.Text = "Minutes";
                }
                StartDatePicker.Value = Core.ViewStart.Date;
                StartTimePicker.Value = Core.ViewStart;
                EndDatePicker.Value = Core.ViewEnd.Date;
                EndTimePicker.Value = Core.ViewEnd;
            }
            UpdateRange();
        }

        /// <summary>
        /// When a user selects an instrument in the tree view of the site 
        /// manager, AddChannelPanels is called to populate the appropriate
        /// channel panels.</summary>
        private void AddChannelPanels(Instrument inst)
        {
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            Core.ActivateInstrument(inst);
            System.Windows.Forms.Cursor.Current = Cursors.Default;

            ChannelsPanel.SuspendLayout();

            foreach (Channel ch in inst.GetChannels())
            {
                if (!ch.Hidden)
                {
                    ChannelPanel chanPan = new ChannelPanel(ch);
                    chanPan.Dock = DockStyle.Top;
                    chanPan.CheckChanged += new EventHandler(OnChannelPannelCheckChanged);
                    ChannelsPanel.Controls.Add(chanPan);
                    chPanels.Add(chanPan);
                }
            }

            for (int i = chPanels.Count - 1; i >= 0; i--)
                chPanels[i].SendToBack();

            ChannelsLabelPanel.SendToBack();
            ChannelsPanel.ResumeLayout();
            UpdateGlobalStartEnd();
        }

        private void RemoveChannelPanels(Instrument inst)
        {
            Core.DeactivateInstrument(inst);

            List<ChannelPanel> chToGo = new List<ChannelPanel>();
            foreach (ChannelPanel chanPan in chPanels)
            {
                if (Object.ReferenceEquals(chanPan.GetChannel().GetInstrument(), inst))
                {
                    chToGo.Add(chanPan);
                }
            }
            foreach(ChannelPanel chanPan in chToGo)
            {
                ChannelsPanel.Controls.Remove(chanPan);
                chPanels.Remove(chanPan);
            }
            chToGo.Clear();
            UpdateGlobalStartEnd();
            UpdatesCharts();
        }

        private void UncheckParentNodes(TreeNode node)
        {
            if (node.Parent != null)
            {
                if (node.Parent.Checked)
                {
                    node.Parent.Checked = false;
                }
                UncheckParentNodes(node.Parent);
            }
        }

        bool checkingChildren = false;
        bool checkingParents = false;
        /// <summary>
        /// Determines which instrument was selected or deselected and
        /// either adds or removes channel panels as appropriate.</summary>
        private void SitesTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if ( e.Node.Tag is Instrument)
            {
                Instrument inst = (Instrument)e.Node.Tag;
                if (e.Node.Checked)
                    AddChannelPanels(inst);
                else
                {
                    RemoveChannelPanels(inst);
                    checkingParents = true;
                    UncheckParentNodes(e.Node);
                    checkingParents = false;
                }
            }
            else if(e.Node.Tag is EventGenerator)
            {
                EventGenerator eventGenerator = (EventGenerator)e.Node.Tag;
                if (e.Node.Checked)
                    Core.ActivateEventGenerator(eventGenerator);
                else
                {
                    Core.DeactivateEventGenerator(eventGenerator);
                    checkingParents = true;
                    UncheckParentNodes(e.Node);
                    checkingParents = false;
                }
            }
            else
            {
                if(!checkingChildren && !checkingParents)
                {
                    checkingChildren = true;
                    // Check/uncheck all children
                    if (e.Node.Checked)
                        CheckChildrenNodes(e.Node);
                    else
                    {
                        UncheckChildrenNodes(e.Node);
                        checkingParents = true;
                        UncheckParentNodes(e.Node);
                        checkingParents = false;
                    }
                    checkingChildren = false;
                }
            }
            CheckFullNodes();
        }

        private void UpdatesCharts()
        {
            StripChartsPanel.SuspendLayout();
            for (int i = 0; i < N_CHARTS; ++i)
                UpdateChart(i);
            StripChartsPanel.ResumeLayout();
        }

        private void StartDatePicker_ValueChanged(object sender, EventArgs e)
        {
            rangeChanged = true;
        }

        private void StartTimePicker_ValueChanged(object sender, EventArgs e)
        {
            rangeChanged = true;
        }

        /// <summary>
        /// Changes the viewed end date when either the start date or interval
        /// changes.</summary>
        private void UpdateEndPickers()
        {
            int range = int.Parse(RangeTextBox.Text);
            DateTime newEnd;
            StartDatePicker.Value = StartDatePicker.Value.Date;
            switch (RangeComboBox.Text)
            {
                case "Years":
                    newEnd = StartDatePicker.Value.AddYears(range);
                    break;
                case "Months":
                    newEnd = StartDatePicker.Value.AddMonths(range);
                    break;
                case "Days":
                    newEnd = StartDatePicker.Value.AddDays(range);
                    break;
                case "Hours":
                    newEnd = StartDatePicker.Value.AddHours(range);
                    break;
                case "Minutes":
                    newEnd = StartDatePicker.Value.AddMinutes(range);
                    break;
                case "Seconds":
                    newEnd = StartDatePicker.Value.AddSeconds(range);
                    break;
                default:
                    newEnd = StartDatePicker.Value.AddMinutes(range);
                    break;
            }
            newEnd = newEnd.AddTicks(StartTimePicker.Value.TimeOfDay.Ticks);
            Core.ChangeView(StartDatePicker.Value.Date.AddTicks(StartTimePicker.Value.TimeOfDay.Ticks),
                newEnd);
            EndDatePicker.Value = Core.ViewEnd.Date;
            EndTimePicker.Value = Core.ViewEnd;
        }

        /// <summary>
        /// Changes the x-axis of the charts whenever the viewing range
        /// changes.</summary>
        private void UpdateRange()
        {
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            if (bootingUp) return;
            if (RangeTextBox.Text == "") return;

            int range;
            try
            {
                range = int.Parse(RangeTextBox.Text);
                UpdateEndPickers();
            }
            catch
            {
                RangeTextBox.Text = "1";
                UpdateEndPickers();
            }

            StripChartsPanel.SuspendLayout();

            // Setup x-axis format
            DateTime start = Core.ViewStart;
            DateTime end = Core.ViewEnd;

            if (start >= end) return;
            int startSeconds = DateTimeToReferenceSeconds(start);
            int endSeconds = DateTimeToReferenceSeconds(end);
            // Update Scrollbar
            StripChartScroll.Minimum =  DateTimeToReferenceSeconds(Core.GlobalStart);
            StripChartScroll.Maximum = DateTimeToReferenceSeconds(Core.GlobalEnd);
            if (startSeconds < StripChartScroll.Minimum) startSeconds = StripChartScroll.Minimum;
            StripChartScroll.Value = startSeconds;
            StripChartScroll.SmallChange = endSeconds - startSeconds;
            StripChartScroll.LargeChange = endSeconds - startSeconds;

            string xLabelFormat;

            DateTimeIntervalType intervalType;
            double daysInRange = TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalDays;
            // Choose an appropriate x-axis label format
            if (daysInRange > 1460)
            {
                xLabelFormat = "yyyy";
                intervalType = DateTimeIntervalType.Years;
                //sep.Step = TimeSpan.FromDays(365).Ticks;
            }
            else if (daysInRange > 540)
            {
                xLabelFormat = "MM/yyyy";
                intervalType = DateTimeIntervalType.Months;
                //sep.Step = TimeSpan.FromDays(90).Ticks;
            }
            else if (daysInRange > 180)
            {
                xLabelFormat = "MM/yyyy";
                intervalType = DateTimeIntervalType.Months;
                //sep.Step = TimeSpan.FromDays(30).Ticks;
            }
            else if (daysInRange > 60)
            {
                xLabelFormat = "MMM dd";
                intervalType = DateTimeIntervalType.Days;
                //sep.Step = TimeSpan.FromDays(10).Ticks;
            }
            else if (daysInRange > 20)
            {
                xLabelFormat = "MMM dd";
                intervalType = DateTimeIntervalType.Days;
                //sep.Step = TimeSpan.FromDays(3).Ticks;
            }
            else if (daysInRange > 2)
            {
                xLabelFormat = "MMM dd";
                intervalType = DateTimeIntervalType.Days;
                //sep.Step = TimeSpan.FromDays(1).Ticks;
            }
            else
            {
                double hoursInRange = TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalHours;
                if (hoursInRange > 12)
                {
                    xLabelFormat = "HH:mm";
                    intervalType = DateTimeIntervalType.Hours;
                    //sep.Step = TimeSpan.FromHours(2).Ticks;
                }
                else if (hoursInRange > 4)
                {
                    xLabelFormat = "HH:mm";
                    intervalType = DateTimeIntervalType.Hours;
                    //sep.Step = TimeSpan.FromHours(1).Ticks;
                }
                else if (hoursInRange > 1)
                {
                    xLabelFormat = "HH:mm";
                    intervalType = DateTimeIntervalType.Minutes;
                    //sep.Step = TimeSpan.FromHours(0.25).Ticks;
                }
                else
                {
                    double minInRange = TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalMinutes;
                    if (minInRange > 30)
                    {
                        xLabelFormat = "HH:mm:ss";
                        intervalType = DateTimeIntervalType.Minutes;
                        //sep.Step = TimeSpan.FromMinutes(5).Ticks;
                    }
                    else if (minInRange > 15)
                    {
                        xLabelFormat = "HH:mm:ss";
                        intervalType = DateTimeIntervalType.Minutes;
                        //sep.Step = TimeSpan.FromMinutes(2).Ticks;
                    }
                    else if (minInRange > 5)
                    {
                        xLabelFormat = "HH:mm:ss";
                        intervalType = DateTimeIntervalType.Minutes;
                        //sep.Step = TimeSpan.FromMinutes(1).Ticks;
                    }
                    else if (minInRange > 1)
                    {
                        xLabelFormat = "HH:mm:ss";
                        intervalType = DateTimeIntervalType.Seconds;
                        //sep.Step = TimeSpan.FromMinutes(0.5).Ticks;
                    }
                    else
                    {
                        xLabelFormat = "HH:mm:ss";
                        intervalType = DateTimeIntervalType.Seconds;
                        //sep.Step = TimeSpan.FromMilliseconds(5000).Ticks;
                    }
                }
            }
            
            for (int i = 0; i < N_CHARTS; ++i)
            {
                Chart chart = GetChart(i);
                chart.ChartAreas[0].AxisX.LabelStyle.Format = xLabelFormat;
                chart.ChartAreas[0].AxisX.IntervalType = intervalType;
                chart.ChartAreas[0].AxisX.Minimum = start.ToOADate();
                chart.ChartAreas[0].AxisX.Maximum = end.ToOADate();
            }
            System.Windows.Forms.Cursor.Current = Cursors.Default;
            UpdatesCharts();
            DrawSections();
            StripChartsPanel.ResumeLayout();
        }

        private void RangeTextBox_TextChanged(object sender, EventArgs e)
        {
            rangeChanged = true;
        }

        private void RangeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (rangeChanged & e.KeyCode == Keys.Enter)
            {
                UpdateRange();
            }
            rangeChanged = false;
        }

        private void RangeUpdateButton_Click(object sender, EventArgs e)
        {
            if (rangeChanged)
            {
                UpdateRange();
            }
            rangeChanged = false;
        }

        private void StripChartScroll_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.EndScroll) return;
            if ((e.NewValue + StripChartScroll.LargeChange >= StripChartScroll.Maximum) &&
                (e.NewValue > e.OldValue))
            {
                e.NewValue = StripChartScroll.Maximum - StripChartScroll.LargeChange;
            }
            Core.ShiftView(ReferenceSecondsToDateTime(e.NewValue) - ReferenceSecondsToDateTime(e.OldValue));
        }

        /// <summary>
        /// When a user clicks on a chart, DrawMarker puts a 
        /// marker/indicator/cursor on the same spot on all of the charts.</summary>
        private void DrawMarker()
        {
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            DrawSections();
            DateTime markerTime = DateTime.FromOADate(markerValue);
            MarkerToolStripLabel.Text = "Marker Location: " + markerTime.ToString("MMM dd, yyyy  HH:mm:ss");
            System.Windows.Forms.Cursor.Current = Cursors.Default;
        }

        private void CreateChartContextMenu()
        {
            ContextMenu chartMenu = new ContextMenu();

            // ID the active chart
            int chartNum = 0;
            for (int i = 0; i < N_CHARTS; i++)
                if (activeChart == GetChart(i)) chartNum = i;

            // Set Y-Axis range
            MenuItem ChartOptionsMenuItem = new MenuItem("Set Y-Axis Range");
            ChartOptionsMenuItem.Tag = chartNum;
            ChartOptionsMenuItem.Click += ChartOptionsMenuItem_Click;
            chartMenu.MenuItems.Add(ChartOptionsMenuItem);

            // Toggle auto scale
            if (autoScale[chartNum])
            {
                MenuItem menuItem = new MenuItem("Do not auto-scale Y-Axis");
                menuItem.Tag = chartNum;
                menuItem.Click += ToggleYAxisAutoScale;
                chartMenu.MenuItems.Add(menuItem);
            }
            else
            {
                MenuItem menuItem = new MenuItem("Auto-scale Y-Axis");
                menuItem.Tag = chartNum;
                menuItem.Click += ToggleYAxisAutoScale;
                chartMenu.MenuItems.Add(menuItem);
            }

            // Toggle log scale
            if (logScale[chartNum])
            {
                MenuItem menuItem = new MenuItem("Switch to linear Y-Axis");
                menuItem.Tag = chartNum;
                menuItem.Click += ToggleYAxisLogScale;
                chartMenu.MenuItems.Add(menuItem);
            }
            else
            {
                MenuItem menuItem = new MenuItem("Switch to log Y-Axis");
                menuItem.Tag = chartNum;
                menuItem.Click += ToggleYAxisLogScale;
                chartMenu.MenuItems.Add(menuItem);
            }

            // Determine which MCAInstruments are being plotted on the chart
            foreach (ChannelPanel chanPan in chPanels)
            {
                Channel chan = chanPan.GetChannel();

                if (chan.GetInstrument() is MCAInstrument || chan.GetInstrument() is DeclarationInstrument)
                {
                    bool plotChan = false;
                    switch (chartNum)
                    {
                        case 0:
                            if (chanPan.Chart1CheckBox.Checked)
                                plotChan = true;
                            break;
                        case 1:
                            if (chanPan.Chart2CheckBox.Checked)
                                plotChan = true;
                            break;
                        case 2:
                            if (chanPan.Chart3CheckBox.Checked)
                                plotChan = true;
                            break;
                        case 3:
                            if (chanPan.Chart4CheckBox.Checked)
                                plotChan = true;
                            break;
                    }
                    if (plotChan)
                    {
                        // Determine whether the user clicked within a measurement
                        List<DateTime> timeStamps = chan.GetTimeStamps(ChannelCompartment.View);
                        List<TimeSpan> durations = chan.GetDurations(ChannelCompartment.View);
                        for(int meas = 0; meas < timeStamps.Count(); meas++)
                        {
                            if (timeStamps[meas] <= mouseTime && mouseTime <= timeStamps[meas] + durations[meas])
                            {
                                if (chan.GetInstrument() is MCAInstrument)
                                {
                                    MenuItem menuItem = new MenuItem("View " + chan.Name + " in Inspectrum");
                                    menuItem.Tag = chan.GetFiles(ChannelCompartment.View)[meas].FileName;
                                    menuItem.Click += PlotSpectrumMenuItem_Click;
                                    chartMenu.MenuItems.Add(menuItem);
                                }
                                else if(chan.GetInstrument() is DeclarationInstrument)
                                {
                                    MenuItem menuItem = new MenuItem("View " + chan.Name + " in Declaration Editor");
                                    menuItem.Tag = chan.GetFiles(ChannelCompartment.View)[meas].FileName;
                                    menuItem.Click += DeclarationMenuItem_Click;
                                    chartMenu.MenuItems.Add(menuItem);
                                }
                            }
                        }
                    }
                }
            }
            chartMenu.Show(activeChart, new Point((int)mouseX, (int)mouseY));
        }

        private void ChartOptionsMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            int chartNum = (int)menuItem.Tag;
            ChartOptionsDialog chartOptionsDialog = new ChartOptionsDialog(chartNum, chartMinY[chartNum], chartMaxY[chartNum]);
            if (chartOptionsDialog.ShowDialog() == DialogResult.OK)
            {
                chartMinY[chartNum] = chartOptionsDialog.YMin;
                chartMaxY[chartNum] = chartOptionsDialog.YMax;
                autoScale[chartNum] = false;
                UpdateChart(chartNum);
            }
        }

        private void ToggleYAxisAutoScale(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            int chartNum = (int)menuItem.Tag;
            autoScale[chartNum] = !autoScale[chartNum];
            UpdateChart(chartNum);
        }

        private void ToggleYAxisLogScale(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            int chartNum = (int)menuItem.Tag;
            logScale[chartNum] = !logScale[chartNum];
            switch(chartNum)
            {
                case 0:
                    C1LogScaleCheckBox.Checked = logScale[chartNum];
                    break;
                case 1:
                    C2LogScaleCheckBox.Checked = logScale[chartNum];
                    break;
                case 2:
                    C3LogScaleCheckBox.Checked = logScale[chartNum];
                    break;
                case 3:
                    C4LogScaleCheckBox.Checked = logScale[chartNum];
                    break;
            }
            UpdateChart(chartNum);
        }

        private void DeclarationMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            DeclarationEditor declarationEditor = new DeclarationEditor();
            declarationEditor.LoadDECFile(menuItem.Tag.ToString());
            declarationEditor.Show();
        }

        private void PlotSpectrumMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Inspectrum inspectrum = new Inspectrum();
            inspectrum.LoadSpectrumFile(menuItem.Tag.ToString());
            inspectrum.Show();
        }

        /// <summary>
        /// Called when a user clicks on any of the charts.</summary>
        private void StripChart_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            drawStatisticsBox = false;

            if (e.Button == MouseButtons.Left)
            {
                Chart chart = (Chart)sender;
                DateTime clickTime = DateTime.FromOADate( chart.ChartAreas[0].AxisX.PixelPositionToValue(mouseX));
                markerValue = clickTime.ToOADate();
                showMarker = true;

                DrawMarker();
            }
            else if (e.Button == MouseButtons.Right)
            {
                CreateChartContextMenu();
            }
        }

        /// <summary>
        /// Called when a user moves the mouse over any of the charts.</summary>
        public void StripChart_MouseMoved(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Chart chart = (Chart)sender;
            activeChart = chart;
            mouseX = e.X;
            mouseY = e.Y;
            try
            {
                mouseTime = DateTime.FromOADate(chart.ChartAreas[0].AxisX.PixelPositionToValue(mouseX));
                MouseTimeToolStripLabel.Text = "Mouse Location: " + mouseTime.ToString("MMM dd, yyyy  HH:mm:ss");
            }
            catch
            { }
            if (drawingZoomBox)
            {
                for (int i=0; i<N_CHARTS; i++)
                {
                    GetChart(i).Invalidate();
                }
            }
        }

        private void C1LogScaleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            logScale[0] = C1LogScaleCheckBox.Checked;
            UpdateChart(0);
        }

        private void C2LogScaleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            logScale[1] = C2LogScaleCheckBox.Checked;
            UpdateChart(1);
        }

        private void C3LogScaleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            logScale[2] = C3LogScaleCheckBox.Checked;
            UpdateChart(2);
        }

        private void C4LogScaleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            logScale[3] = C4LogScaleCheckBox.Checked;
            UpdateChart(3);
        }

        private void RangeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            rangeChanged = true;
        }

        private void SiteManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SiteManagerForm siteManForm = new SiteManagerForm(this, Core.SiteManager);
            siteManForm.ShowDialog();
        }

        private void PresetsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyPreset(PresetsComboBox.Text);
        }

        /// <summary>
        /// Called when a user selects a preset from the preset ComboBox.</summary>
        private void ApplyPreset(string presetName)
        {
            Preset preset = presetMan.GetPresets().Single(p => p.GetName() == presetName);
            ClearPanels();

            foreach (Instrument inst in preset.GetActiveInstruments())
            {
                SitesTreeView.Nodes.Find(inst.Name, true)[0].Checked = true;
            }

            // So, this next part could probably be more elegant...
            int c = 0;
            foreach (Channel chan in preset.GetActiveChannels()[c])
            {
                chPanels.Single(cp => cp.GetChannel().Equals(chan)).Chart1CheckBox.Checked = true;
            }

            c = 1;
            foreach (Channel chan in preset.GetActiveChannels()[c])
            {
                chPanels.Single(cp => cp.GetChannel().Equals(chan)).Chart2CheckBox.Checked = true;
            }

            c = 2;
            foreach (Channel chan in preset.GetActiveChannels()[c])
            {
                chPanels.Single(cp => cp.GetChannel().Equals(chan)).Chart3CheckBox.Checked = true;
            }

            c = 3;
            foreach (Channel chan in preset.GetActiveChannels()[c])
            {
                chPanels.Single(cp => cp.GetChannel().Equals(chan)).Chart4CheckBox.Checked = true;
            }
            // ... whatever.

            foreach (EventGenerator eventGenerator in preset.GetActiveEventGenerators())
            {
                SitesTreeView.Nodes.Find(eventGenerator.Name, true)[0].Checked = true;
            }

            PresetNameTextBox.Text = preset.GetName();
        }

        private void SavePreset(string newName)
        {
            if (newName == "") return;

            // Remove preset with the same name, if it exists
            int indexToDelete = -1;
            for (int p=0; p <presetMan.GetPresets().Count; p++)
                if (presetMan.GetPresets()[p].GetName() == newName)
                    indexToDelete = p;
            if (indexToDelete >= 0)
            {
                presetMan.GetPresets().RemoveAt(indexToDelete);
            }

            // Make the new preset
            Preset preset = new Preset(newName);
            foreach (Instrument inst in Core.ActiveInstruments)
                preset.GetActiveInstruments().Add(inst);
            foreach (ChannelPanel cp in chPanels)
            {
                if (cp.Chart1CheckBox.Checked)
                    preset.AddChannel(cp.GetChannel(), 0);
                if (cp.Chart2CheckBox.Checked)
                    preset.AddChannel(cp.GetChannel(), 1);
                if (cp.Chart3CheckBox.Checked)
                    preset.AddChannel(cp.GetChannel(), 2);
                if (cp.Chart4CheckBox.Checked)
                    preset.AddChannel(cp.GetChannel(), 3);
            }

            //  Put all of the checked systems in the SitesTreeView in eventWatchers
            foreach (TreeNode siteNode in SitesTreeView.Nodes)
            {
                foreach (TreeNode facNode in siteNode.Nodes)
                {
                    foreach (TreeNode sysNode in facNode.Nodes)
                    {
                        foreach (TreeNode node in sysNode.Nodes)
                        {
                            if (node.Tag is EventGenerator && node.Checked)
                            {
                                preset.GetActiveEventGenerators().Add((EventGenerator)node.Tag);
                            }
                        }
                    }
                }
            }

            if (indexToDelete >= 0)
            {
                presetMan.GetPresets().Insert(indexToDelete, preset);
            }
            else
            {
                presetMan.GetPresets().Add(preset);
                PresetsComboBox.Items.Add(preset.GetName());
            }

            // Save the new preset to xml
            presetMan.Save();
        }

        private void PresetSaveButton_Click(object sender, EventArgs e)
        {
            SavePreset(PresetNameTextBox.Text);
        }

        private void GenerateEventsButton_Click(object sender, EventArgs e)
        {
            GenerateEvents(Core.GlobalStart, Core.GlobalEnd);
        }

        private void GenerateEvents(DateTime start, DateTime end)
        {
            Core.GenerateEvents(start, end);

            EventGridView.Rows.Clear();
            for (int i=0; i < Core.Events.Count(); i++)
            {
                EventGridView.Rows.Add(
                    Core.Events[i].GetEventGenerator().Name,
                    Core.Events[i].GetStartTime().ToString("MM/dd/yy HH:mm:ss"),
                    Core.Events[i].GetEndTime().ToString("MM/dd/yy HH:mm:ss"),
                    Core.Events[i].GetDuration().TotalSeconds,
                    Core.Events[i].GetMaxValue(),
                    Core.Events[i].GetMaxTime().ToString("MM/dd/yy HH:mm:ss"),
                    Core.Events[i].GetComment());
                EventGridView.Rows[i].Tag = Core.Events[i];
            }
            if (HighlightEventsCheckBox.Checked)
                DrawSections();
        }

        private void HighlightEventsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            DrawSections();
        }

        private void EventManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EventManagerForm eventManForm = new EventManagerForm(this, Core.SiteManager);
            eventManForm.ShowDialog();
        }

        private void launchInspectaclesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Inspectacles inspectacles = new Inspectacles();
            inspectacles.Show();
        }

        private void EventGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (EventGridView.Rows[e.RowIndex].Tag is Event)
            {
                EventViewerForm eventViewerForm = new EventViewerForm((Event)EventGridView.Rows[e.RowIndex].Tag);
                eventViewerForm.Show();
            }
        }

        private void declarationEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeclarationEditor declarationEditor = new DeclarationEditor();
            declarationEditor.Show();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(Core.ActiveInstruments.Count == 0)
            {
                MessageBox.Show("No active Instruments for exporting data!");
                return;
            }
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;

            ExportDataDialog dialog = new ExportDataDialog(Core.ActiveInstruments, Core.GlobalStart, Core.GlobalEnd,
                StartDatePicker.Value, StartTimePicker.Value, EndDatePicker.Value, EndTimePicker.Value);
            dialog.ShowDialog();
            return;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox(OmniscientCore.VERSION);
            aboutBox.ShowDialog();
        }

        private int DateTimeToReferenceSeconds(DateTime dateTime)
        {
            DateTime reference = new DateTime(2000, 1, 1, 0, 0, 0);
            return (int)((dateTime.Ticks - reference.Ticks)/1e7);
        }

        private DateTime ReferenceSecondsToDateTime(int time)
        {
            DateTime reference = new DateTime(2000, 1, 1, 0, 0, 0);
            return reference.AddSeconds(time);
        }

        private void StripChart_DoubleClick(object sender, EventArgs e)
        {
            Chart chart = (Chart)sender;
            using (MemoryStream ms = new MemoryStream())
            {
                chart.SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                Clipboard.SetImage(bm);
            }
        }

        Chart downChart;
        bool drawingZoomBox = false;
        bool drawStatisticsBox = false;
        private void StripChart_MouseDown(object sender, MouseEventArgs e)
        {
            downChart = (Chart)sender;
            mouseDownX = downChart.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
            if (e.Button == MouseButtons.Left)
            {
                drawStatisticsBox = false;
                drawingZoomBox = true;
            }
        }

        string boxData;
        private void CalculateHighlightBoxValues()
        {
            boxData = "";
            int chartNum = (int)downChart.Tag;
            DateTime start = DateTime.FromOADate(mouseDownX);
            DateTime end = DateTime.FromOADate(mouseUpX);
            if (end < start)
            {
                DateTime dateTimesScrap = start;
                start = end;
                end = dateTimesScrap;
            }
            foreach (ChannelPanel chanPan in chPanels)
            {
                bool plotChan = false;
                switch (chartNum)
                {
                    case 0:
                        if (chanPan.Chart1CheckBox.Checked)
                            plotChan = true;
                        break;
                    case 1:
                        if (chanPan.Chart2CheckBox.Checked)
                            plotChan = true;
                        break;
                    case 2:
                        if (chanPan.Chart3CheckBox.Checked)
                            plotChan = true;
                        break;
                    case 3:
                        if (chanPan.Chart4CheckBox.Checked)
                            plotChan = true;
                        break;    
                }
                if(plotChan)
                {
                    Channel ch = chanPan.GetChannel();
                    boxData += "--" + ch.Name + "--\n";
                    boxData += "μ:\t" + ch.GetAverage(ChannelCompartment.View, start, end).ToString("G6") + "\n";
                    boxData += "σ:\t" + ch.GetStandardDeviation(ChannelCompartment.View, start, end).ToString("G6") + "\n";
                    boxData += "Max:\t" + ch.GetMax(ChannelCompartment.View, start, end).ToString("G6") + "\n";
                    boxData += "Min:\t" + ch.GetMin(ChannelCompartment.View, start, end).ToString("G6") + "\n";
					boxData += "\n";
                }
            }
        }

        private void StripChart_MouseUp(object sender, MouseEventArgs e)
        {
            Chart chart = (Chart)sender;
            if (chart != downChart) return;
            mouseUpX = 0;
            try
            {
                mouseUpX = chart.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
            }
            catch
            {
                return;
            }
            drawingZoomBox = false;
            
            double mouseDelta = mouseUpX - mouseDownX;
            double range = chart.ChartAreas[0].AxisX.Maximum - chart.ChartAreas[0].AxisX.Minimum;

            if (e.Button == MouseButtons.Right)
            {
                // Drag Range
                Core.ShiftView(DateTime.FromOADate(chart.ChartAreas[0].AxisX.Minimum) -
                    DateTime.FromOADate(chart.ChartAreas[0].AxisX.Minimum+mouseDelta));
            }
            else if (mouseDelta/range < -0.05 && !controlPressed)
            {
                // Zoom out
                Core.ChangeView(DateTime.FromOADate(chart.ChartAreas[0].AxisX.Minimum - range / 2),
                    DateTime.FromOADate(chart.ChartAreas[0].AxisX.Maximum + range / 2));
            }
            else if (mouseDelta / range > 0.05 && !controlPressed)
            {
                // Zoom in
                Core.ChangeView(DateTime.FromOADate(mouseDownX),
                    DateTime.FromOADate(mouseUpX));
            }
            else if (!controlPressed || 
                ((mouseDelta / range < 0.05) && (mouseDelta/range > -0.05)))
            {
                // This is nothing - just do nothing
                drawingZoomBox = false;
                drawStatisticsBox = false;
                return;
            }
            else
            {
                // Draw statistics box
                drawStatisticsBox = true;
                CalculateHighlightBoxValues();
                return;
            }
        }

        public void StripChart_Paint(object sender, PaintEventArgs e)
        {
            if (drawingZoomBox || drawStatisticsBox)
            {
                Chart chart = (Chart)sender;
                Axis X = chart.ChartAreas[0].AxisX;
                Axis Y = chart.ChartAreas[0].AxisY;
                int xStart = (int)X.ValueToPixelPosition(mouseDownX);
                int xNow = (int)mouseX;
                e.Graphics.DrawRectangle(Pens.Gray, Math.Min(xStart, xNow), (int)Y.ValueToPixelPosition(Y.Maximum),
                    Math.Abs(xStart - xNow), (int)Y.ValueToPixelPosition(Y.Minimum) - (int)Y.ValueToPixelPosition(Y.Maximum));
                if(drawStatisticsBox && chart == downChart)
                {
                    e.Graphics.DrawString(boxData, SystemFonts.DefaultFont, SystemBrushes.InfoText, new Point(Math.Min(xStart, xNow) + 5, (int)Y.ValueToPixelPosition(Y.Maximum) + 5));
                }
            }
        }
        
        bool controlPressed = false;
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                controlPressed = true;
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                controlPressed = false;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Core.Shutdown();
        }
    }
}
