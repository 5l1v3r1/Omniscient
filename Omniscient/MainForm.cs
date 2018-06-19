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

using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.WinForms;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Configurations;

namespace Omniscient
{
    /// <summary>
    /// MainForm is the window that appears when opening Omniscient as is where most of the user-interaction occurs.
    /// </summary>
    public partial class MainForm : Form
    {
        ///////////////////////////////////////////////////////////////////////
        private const string VERSION = "0.2.0";

        private const int N_CHARTS = 4;
        private const int MAX_HIGHLIGHTED_EVENTS = 60;

        public SiteManager siteMan;
        public PresetManager presetMan;
        List<ChannelPanel> chPanels;
        List<Instrument> activeInstruments;

        private DateTime globalStart;
        private DateTime globalEnd;

        private bool rangeChanged = false;
        private bool bootingUp = false;

        private bool[] logScale;

        // The following are used to show a time marker when a user clicks a chart
        LiveCharts.WinForms.CartesianChart activeChart;
        double mouseX = 0;
        double mouseY = 0;
        DateTime mouseTime;
        private bool showMarker = false;
        private double markerValue = 0;

        List<Event> events;
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// MainForm constructor
        /// </summary>
        public MainForm()
        {
            logScale = new bool[N_CHARTS];
            for (int c = 0; c < N_CHARTS; c++) logScale[c] = false;
            activeInstruments = new List<Instrument>();
            events = new List<Event>();
            InitializeComponent();
        }

        /// <summary>
        /// MainForm_Load initializes several variables after the form is constructed.</summary>
        /// <remarks>Event hanlders for mouse events on the charts are handled 
        /// here because they cause errors when put in the form designer. </remarks>
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = "Omniscient - Version " + VERSION;
            bootingUp = true;
            this.StripChart0.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart1.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart2.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart3.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart0.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);
            this.StripChart1.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);
            this.StripChart2.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);
            this.StripChart3.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);

            globalStart = DateTime.Today.AddDays(-1);
            GlobalStartTextBox.Text = globalStart.ToString("MMM dd, yyyy");
            globalEnd = DateTime.Today;
            GlobalEndTextBox.Text = globalEnd.ToString("MMM dd, yyyy");
            StartDatePicker.Value = globalStart;
            EndDatePicker.Value = globalEnd;
            StartTimePicker.Value = globalStart.Date;
            EndTimePicker.Value = globalEnd.Date;
            chPanels = new List<ChannelPanel>();
            siteMan = new SiteManager("SiteManager.xml");
            ReturnCode returnCode = siteMan.Reload();
            if (returnCode == ReturnCode.FILE_DOESNT_EXIST)
            {
                siteMan.WriteBlank();
                MessageBox.Show("SiteManager.xml not found. A new one has been created.");
            }
            else if (returnCode != ReturnCode.SUCCESS) MessageBox.Show("Warning: Bad trouble loading the site manager!");
            LoadPresets();
            UpdateSitesTree();
            RangeTextBox.Text = "1";
            RangeComboBox.Text = "Days";
            InitializeCharts();
            bootingUp = false;
            UpdateRange();
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

            while (activeInstruments.Count>0)
            {
                //SitesTreeView.Nodes.Find(activeInstruments[0].GetName(), true)[0].Checked = false;
                RemoveChannelPanels(activeInstruments[0]);
            }
        }
        
        public void LoadPresets()
        {
            presetMan = new PresetManager("Presets.xml", siteMan);
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
            foreach (Site site in siteMan.GetSites())
            {
                TreeNode siteNode = new TreeNode(site.GetName());
                siteNode.Name = site.GetName();
                siteNode.Tag = site;
                siteNode.ImageIndex = 0;
                siteNode.SelectedImageIndex = 0;
                siteNode.ToolTipText = siteNode.Text;
                foreach (Facility fac in site.GetFacilities())
                {
                    TreeNode facNode = new TreeNode(fac.GetName());
                    facNode.Name = fac.GetName();
                    facNode.Tag = fac;
                    facNode.ImageIndex = 1;
                    facNode.SelectedImageIndex = 1;
                    facNode.ToolTipText = facNode.Text;
                    foreach (DetectionSystem sys in fac.GetSystems())
                    {
                        TreeNode sysNode = new TreeNode(sys.GetName());
                        sysNode.Name = sys.GetName();
                        sysNode.Tag = sys;
                        sysNode.ImageIndex = 2;
                        sysNode.SelectedImageIndex = 2;
                        sysNode.ToolTipText = sysNode.Text;
                        foreach (Instrument inst in sys.GetInstruments())
                        {
                            TreeNode instNode = new TreeNode(inst.GetName());
                            instNode.Name = inst.GetName();
                            instNode.Tag = inst;
                            instNode.ImageIndex = 3;
                            instNode.SelectedImageIndex = 3;
                            instNode.ToolTipText = instNode.Text;
                            sysNode.Nodes.Add(instNode);
                        }
                        foreach (EventGenerator eg in sys.GetEventGenerators())
                        {
                            TreeNode egNode = new TreeNode(eg.GetName());
                            egNode.Name = eg.GetName();
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
        private LiveCharts.WinForms.CartesianChart GetChart(int chartNum)
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
        /// UpdateInstrumentData is called when instruments are added to the channel panels.</summary>
        private void UpdateInstrumentData(Instrument inst)
        {
            Cursor.Current = Cursors.WaitCursor;
            inst.LoadData(new DateTime(1900, 1, 1), new DateTime(2100, 1, 1));
            Cursor.Current = Cursors.Default;
        }

        private void UpdateData()
        {
            foreach(Instrument inst in activeInstruments)
            {
                UpdateInstrumentData(inst);
            }
        }

        /// <summary>
        /// InitializeCharts is called when the form is loaded. </summary>
        private void InitializeCharts()
        {
            for (int chartNum = 0; chartNum < N_CHARTS; chartNum++)
            {
                LiveCharts.WinForms.CartesianChart chart;
                chart = GetChart(chartNum);

                chart.DisableAnimations = true;
                chart.Hoverable = false;
                chart.DataTooltip = null;

                // Initizialize chart values
                chart.AxisX.Clear();
                chart.AxisY.Clear();
                chart.AxisX.Add(new Axis
                {
                    LabelFormatter = val => new System.DateTime((long)val).ToString("MM DD YYY"),
                    MinValue = DateTime.Today.Ticks,
                    MaxValue = DateTime.Today.Ticks + TimeSpan.TicksPerDay,
                    //Separator = sep
                });

                chart.AxisY.Add(new Axis
                {
                    MinValue = 0,
                    //MaxValue = 200
                });

                GearedValues<DateTimePoint> chartVals = new GearedValues<DateTimePoint>();
                List<DateTimePoint> list = new List<DateTimePoint>();
                list.Add(new DateTimePoint(DateTime.Today, 0.0));
                chartVals = list.AsGearedValues().WithQuality(Quality.Highest);

                GStepLineSeries series = new GStepLineSeries()
                {
                    Title = "",
                    PointGeometry = null,
                    Values = chartVals
                };

                chart.LegendLocation = LegendLocation.Right;
                chart.DefaultLegend.Visibility = System.Windows.Visibility.Visible;
                chart.DefaultLegend.Width = 200;
                chart.Update(true, true);
            }
        }

        private DateTime GetRangeStart()
        {
            DateTime start = StartDatePicker.Value.Date;
            return start.Add(StartTimePicker.Value.TimeOfDay);
        }

        private DateTime GetRangeEnd()
        {
            DateTime end = EndDatePicker.Value.Date;
            return end.Add(EndTimePicker.Value.TimeOfDay);
        }

        /// <summary>
        /// UpdateChart is called whenever the data to be displayed on a chart changes. </summary>
        private void UpdateChart(int chartNum)
        {
            Cursor.Current = Cursors.WaitCursor;
            LiveCharts.WinForms.CartesianChart chart;
            chart = GetChart(chartNum);

            // Needed for speedy loading
            DateTime start = GetRangeStart();
            DateTime end = GetRangeEnd();

            SeriesCollection seriesColl;
            if (logScale[chartNum])
            {
                seriesColl = new SeriesCollection(Mappers.Xy<DateTimePoint>()
                    .X(point => point.DateTime.Ticks)
                    .Y(point => Math.Log10(point.Value)));
                chart.AxisY.Clear();
                chart.AxisY.Add(new LogarithmicAxis()
                {
                    LabelFormatter = value => Math.Pow(10, value).ToString("N0"),
                    Base = 10,
                });
            }
            else
            {
                seriesColl = new SeriesCollection();
                chart.AxisY.Clear();
                chart.AxisY.Add(new Axis()
                {
                    MinValue = 0,
                });
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
                if (plotChan)
                {
                    Channel chan = chanPan.GetChannel();

                    List<DateTime> dates = chan.GetTimeStamps();
                    List<double> vals = chan.GetValues();
                    if (chan.GetChannelType() == Channel.ChannelType.DURATION_VALUE)
                    {
                        List<TimeSpan> durations = chan.GetDurations();
                        ChartValues<DateTimePoint> chartVals = new ChartValues<DateTimePoint>();
                        List<DateTimePoint> list = new List<DateTimePoint>();
                        if (logScale[chartNum])
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (vals[i] > 0 && dates[i] + durations[i] >= start && dates[i] <= end)
                                {
                                    list.Add(new DateTimePoint(dates[i], vals[i]));
                                    list.Add(new DateTimePoint(dates[i] + durations[i], vals[i]));
                                    list.Add(new DateTimePoint(dates[i] + durations[i].Add(TimeSpan.FromTicks(1)), double.NaN));
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (dates[i] + durations[i] >= start && dates[i] <= end)
                                {
                                    list.Add(new DateTimePoint(dates[i], vals[i]));
                                    list.Add(new DateTimePoint(dates[i] + durations[i], vals[i]));
                                    list.Add(new DateTimePoint(dates[i] + durations[i].Add(TimeSpan.FromTicks(1)), double.NaN));
                                }
                            }
                        }
                        chartVals.AddRange(list);
                        LineSeries series;
                        series = new LineSeries()
                        {
                            Title = chan.GetName(),
                            PointGeometry = null,
                            Values = chartVals
                        };
                        seriesColl.Add(series);
                    }
                    else
                    {
                        // Load up the chart values
                        GearedValues<DateTimePoint> chartVals = new GearedValues<DateTimePoint>();
                        List<DateTimePoint> list = new List<DateTimePoint>();
                        if (logScale[chartNum])
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (vals[i] > 0 && dates[i] >= start && dates[i] <= end)
                                    list.Add(new DateTimePoint(dates[i], vals[i]));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (dates[i] >= start && dates[i] <= end)
                                    list.Add(new DateTimePoint(dates[i], vals[i]));
                            }
                        }
                        chartVals = list.AsGearedValues().WithQuality(Quality.Highest);
                        GStepLineSeries series;
                        series = new GStepLineSeries()
                        {
                            Title = chan.GetName(),
                            PointGeometry = null,
                            Values = chartVals
                        };
                        seriesColl.Add(series);
                    }
                }
            }
            chart.Series = seriesColl;
            Cursor.Current = Cursors.Default;
        }

        private void DrawSections()
        {
            DateTime start = GetRangeStart();
            DateTime end = GetRangeEnd();
            int eventCount = 0;
            LiveCharts.WinForms.CartesianChart chart;
            for (int chartNum = 0; chartNum < N_CHARTS; chartNum++)
            {
                chart = GetChart(chartNum);
                if (chart.AxisX[0].Sections != null)
                    chart.AxisX[0].Sections.Clear();
                chart.AxisX[0].Sections = new SectionsCollection();
                chart.AxisX[0].Sections.Add(
                    new AxisSection()
                    {
                        Value = markerValue,
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                        StrokeThickness = 1,
                    }
                );

                eventCount = 0;
                if (HighlightEventsCheckBox.Checked)
                {
                    foreach (Event eve in events)
                    {
                        if (eve.GetStartTime() < end && eve.GetEndTime() > start && eventCount < MAX_HIGHLIGHTED_EVENTS)
                        {
                            chart.AxisX[0].Sections.Add(new AxisSection()
                            {
                                Value = eve.GetStartTime().Ticks,
                                SectionWidth = (eve.GetEndTime().Ticks - eve.GetStartTime().Ticks > 0) ? 
                                                    eve.GetEndTime().Ticks - eve.GetStartTime().Ticks :
                                                    TimeSpan.FromSeconds(5).Ticks,
                                Fill = new SolidColorBrush
                                {
                                    Color = System.Windows.Media.Color.FromRgb(0, 255, 0),
                                    Opacity = .5
                                }
                            });
                            eventCount++;
                        }
                    }
                }
                chart.Update(true, true);
            }
            if (eventCount == MAX_HIGHLIGHTED_EVENTS)
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
                LiveCharts.WinForms.CartesianChart chart = GetChart(chartNum);
                if (chart.Series.Count > 0) numVisible++;
            }
            for (int chartNum = 0; chartNum < N_CHARTS; chartNum++)
            {
                LiveCharts.WinForms.CartesianChart chart = GetChart(chartNum);
                if (chart.Series.Count > 0)
                {
                    StripChartsLayoutPanel.RowStyles[chartNum].SizeType = SizeType.Percent;
                    StripChartsLayoutPanel.RowStyles[chartNum].Height = (float)100.0 / numVisible;
                }
                else
                {
                    StripChartsLayoutPanel.RowStyles[chartNum].Height = 0;
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
            DateTime earliest = new DateTime(3000,1,1);
            DateTime latest = new DateTime(1000, 1, 1);
            DateTime chStart;
            DateTime chEnd;

            if (activeInstruments.Count == 0) return;

            // Figure out the earliest and latest data point
            foreach (Instrument inst in activeInstruments)
            {
                foreach (Channel ch in inst.GetChannels())
                {
                    if(ch.GetTimeStamps().Count > 0)
                    {
                        chStart = ch.GetTimeStamps()[0];
                        chEnd = ch.GetTimeStamps()[ch.GetTimeStamps().Count - 1];
                        if(ch.GetChannelType() == Channel.ChannelType.DURATION_VALUE)
                            chEnd += ch.GetDurations()[ch.GetDurations().Count - 1];
                        if (chStart < earliest)
                            earliest = chStart;
                        if (chEnd > latest)
                            latest = chEnd;
                    }
                }
            }

            if (earliest > latest) return;

            // Update global start and end
            globalStart = earliest;
            globalEnd = latest;
            GlobalStartTextBox.Text = globalStart.ToString("MMM dd, yyyy");
            GlobalEndTextBox.Text = globalEnd.ToString("MMM dd, yyyy");

            // Update the range pickers as needed
            DateTime start = StartDatePicker.Value.Date;
            start = start.Add(StartTimePicker.Value.TimeOfDay);
            DateTime end = EndDatePicker.Value.Date;
            end = end.Add(EndTimePicker.Value.TimeOfDay);

            bool changedRange = false;
            if (start < globalStart || start > globalEnd)
            {
                start = globalStart;
                changedRange = true;
            }
            if (end > globalEnd || end < globalStart)
            {
                end = globalEnd;
                changedRange = true;
            }
            if (changedRange)
            {
                if (TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalDays > 1)
                {
                    start = end.AddDays(-1);
                    RangeTextBox.Text = "1";
                    RangeComboBox.Text = "Days";
                }
                else if (TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalDays > 1)
                {
                    start = end.AddDays(-1);
                    RangeTextBox.Text = "24";
                    RangeComboBox.Text = "Hours";
                }
                else if (TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalHours > 1)
                {
                    start = end.AddHours(-1);
                    RangeTextBox.Text = "60";
                    RangeComboBox.Text = "Minutes";
                }
                else
                {
                    RangeTextBox.Text = "";
                    RangeComboBox.Text = "Minutes";
                }
                StartDatePicker.Value = start.Date;
                StartTimePicker.Value = start;
                EndDatePicker.Value = end.Date;
                EndTimePicker.Value = end;
            }
            UpdateRange();
        }

        /// <summary>
        /// When a user selects an instrument in the tree view of the site 
        /// manager, AddChannelPanels is called to populate the appropriate
        /// channel panels.</summary>
        private void AddChannelPanels(Instrument inst)
        {
            activeInstruments.Add(inst);
            UpdateInstrumentData(inst);

            ChannelsPanel.SuspendLayout();

            foreach (Channel ch in inst.GetChannels())
            {
                ChannelPanel chanPan = new ChannelPanel(ch);
                chanPan.Dock = DockStyle.Top;
                chanPan.CheckChanged += new EventHandler(OnChannelPannelCheckChanged);
                ChannelsPanel.Controls.Add(chanPan);
                chPanels.Add(chanPan);
            }

            for (int i = chPanels.Count - 1; i >= 0; i--)
                chPanels[i].SendToBack();

            ChannelsLabelPanel.SendToBack();
            ChannelsPanel.ResumeLayout();
            UpdateGlobalStartEnd();
        }

        private void RemoveChannelPanels(Instrument inst)
        {
            activeInstruments.Remove(inst);
            inst.ClearData();

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
            if (StartDatePicker.Value.Date < globalStart.Date)
            {
                StartDatePicker.Value = globalStart.Date;
                StartTimePicker.Value = globalStart;
            }
            if (StartDatePicker.Value.Date > globalEnd.Date)
            {
                StartDatePicker.Value = globalEnd.Date;
                StartTimePicker.Value = globalEnd.Date;
            }
            rangeChanged = true;
        }

        private void StartTimePicker_ValueChanged(object sender, EventArgs e)
        {
            if (StartDatePicker.Value.Date.AddTicks(StartTimePicker.Value.TimeOfDay.Ticks) < globalStart)
            {
                StartDatePicker.Value = globalStart.Date;
                StartTimePicker.Value = globalStart;
            }
            if (StartDatePicker.Value.Date.AddTicks(StartTimePicker.Value.TimeOfDay.Ticks) > globalEnd)
            {
                StartDatePicker.Value = globalEnd.Date;
                StartTimePicker.Value = globalEnd.Date;
            }
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
                default:
                    newEnd = StartDatePicker.Value.AddMinutes(range);
                    break;
            }
            newEnd = newEnd.AddTicks(StartTimePicker.Value.TimeOfDay.Ticks);
            if (newEnd > globalEnd)
                newEnd = globalEnd;
            EndDatePicker.Value = newEnd.Date;
            EndTimePicker.Value = newEnd;

        }

        /// <summary>
        /// Changes the x-axis of the charts whenever the viewing range
        /// changes.</summary>
        private void UpdateRange()
        {
            Cursor.Current = Cursors.WaitCursor;
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
            DateTime start = GetRangeStart();
            DateTime end = GetRangeEnd();

            if (start >= end) return;

            // Update Scrollbar
            StripChartScroll.Minimum = (int)(globalStart.Ticks/6e8);
            StripChartScroll.Maximum = (int)(globalEnd.Ticks/6e8);
            StripChartScroll.Value = (int)(start.Ticks/6e8);
            StripChartScroll.SmallChange = (int)((end.Ticks - start.Ticks) / 6e8);
            StripChartScroll.LargeChange = (int)((end.Ticks - start.Ticks) / 6e8);

            string xLabelFormat;
            Separator sep = new Separator();
            double daysInRange = TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalDays;
            // Choose an appropriate x-axis label format
            if (daysInRange > 1460)
            {
                xLabelFormat = "yyyy";
                sep.Step = TimeSpan.FromDays(365).Ticks;
            }
            else if (daysInRange > 540)
            {
                xLabelFormat = "MM/yyyy";
                sep.Step = TimeSpan.FromDays(90).Ticks;
            }
            else if (daysInRange > 180)
            {
                xLabelFormat = "MM/yyyy";
                sep.Step = TimeSpan.FromDays(30).Ticks;
            }
            else if (daysInRange > 60)
            {
                xLabelFormat = "MMM dd";
                sep.Step = TimeSpan.FromDays(10).Ticks;
            }
            else if (daysInRange > 20)
            {
                xLabelFormat = "MMM dd";
                sep.Step = TimeSpan.FromDays(3).Ticks;
            }
            else if (daysInRange > 2)
            {
                xLabelFormat = "MMM dd";
                sep.Step = TimeSpan.FromDays(1).Ticks;
            }
            else
            {
                double hoursInRange = TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalHours;
                if (hoursInRange > 12)
                {
                    xLabelFormat = "HH:mm";
                    sep.Step = TimeSpan.FromHours(2).Ticks;
                }
                else if (hoursInRange > 4)
                {
                    xLabelFormat = "HH:mm";
                    sep.Step = TimeSpan.FromHours(1).Ticks;
                }
                else if (hoursInRange > 1)
                {
                    xLabelFormat = "HH:mm";
                    sep.Step = TimeSpan.FromHours(0.25).Ticks;
                }
                else
                {
                    double minInRange = TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalMinutes;
                    if (minInRange > 30)
                    {
                        xLabelFormat = "HH:mm:ss";
                        sep.Step = TimeSpan.FromMinutes(5).Ticks;
                    }
                    else if (minInRange > 15)
                    {
                        xLabelFormat = "HH:mm:ss";
                        sep.Step = TimeSpan.FromMinutes(2).Ticks;
                    }
                    else if (minInRange > 5)
                    {
                        xLabelFormat = "HH:mm:ss";
                        sep.Step = TimeSpan.FromMinutes(1).Ticks;
                    }
                    else if (minInRange > 1)
                    {
                        xLabelFormat = "HH:mm:ss";
                        sep.Step = TimeSpan.FromMinutes(0.5).Ticks;
                    }
                    else
                    {
                        xLabelFormat = "HH:mm:ss";
                        sep.Step = TimeSpan.FromMilliseconds(5000).Ticks;
                    }
                }
            }

            for (int i = 0; i < N_CHARTS; ++i)
            {
                LiveCharts.WinForms.CartesianChart chart;
                chart = GetChart(i);

                if (chart.AxisX.Count > 0)
                {
                    chart.AxisX[0].LabelFormatter = val => new System.DateTime((long)val).ToString(xLabelFormat);
                    chart.AxisX[0].MinValue = start.Ticks;
                    chart.AxisX[0].MaxValue = end.Ticks;
                    chart.AxisX[0].Separator = sep;
                }
                else
                {
                    chart.AxisX.Clear();
                    chart.AxisX.Add(new Axis
                    {
                        LabelFormatter = val => new System.DateTime((long)val).ToString(xLabelFormat),
                        MinValue = start.Ticks,
                        MaxValue = end.Ticks,
                        Separator = sep
                    });
                }

                chart.AxisY.Clear();
                chart.AxisY.Add(new Axis
                {
                    MinValue = 0,

                    //MaxValue = 200
                });
                
                
            }
            Cursor.Current = Cursors.Default;
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
            DateTime newStart = new DateTime((long)(StripChartScroll.Value * 6e8));
            StartDatePicker.Value = newStart.Date;
            StartTimePicker.Value = newStart;
            UpdateEndPickers();
            UpdateRange();
        }

        /// <summary>
        /// When a user clicks on a chart, DrawMarker puts a 
        /// marker/indicator/cursor on the same spot on all of the charts.</summary>
        private void DrawMarker()
        {
            Cursor.Current = Cursors.WaitCursor;
            DrawSections();
            DateTime markerTime = new DateTime((long)markerValue);
            MarkerToolStripLabel.Text = "Marker Location: " + markerTime.ToString("MMM dd, yyyy  HH:mm:ss");
            Cursor.Current = Cursors.Default;
        }

        private void CreateChartContextMenu()
        {
            ContextMenu chartMenu = new ContextMenu();

            // ID the active chart
            int chartNum = 0;
            for (int i = 0; i < N_CHARTS; i++)
                if (activeChart == GetChart(i)) chartNum = i;

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
                        List<DateTime> timeStamps = chan.GetTimeStamps();
                        List<TimeSpan> durations = chan.GetDurations();
                        for(int meas = 0; meas < timeStamps.Count(); meas++)
                        {
                            if (timeStamps[meas] <= mouseTime && mouseTime <= timeStamps[meas] + durations[meas])
                            {
                                if (chan.GetInstrument() is MCAInstrument)
                                {
                                    MenuItem menuItem = new MenuItem("View " + chan.GetName() + " in Inspectrum");
                                    menuItem.Tag = chan.GetFiles()[meas].FileName;
                                    menuItem.Click += PlotSpectrumMenuItem_Click;
                                    chartMenu.MenuItems.Add(menuItem);
                                }
                                else if(chan.GetInstrument() is DeclarationInstrument)
                                {
                                    MenuItem menuItem = new MenuItem("View " + chan.GetName() + " in Declaration Editor");
                                    menuItem.Tag = chan.GetFiles()[meas].FileName;
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
        private void StripChart_MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                LiveCharts.Wpf.CartesianChart chartBase = (LiveCharts.Wpf.CartesianChart)sender;
                DateTime clickTime = new DateTime((long)LiveCharts.ChartFunctions.FromPlotArea(mouseX, AxisOrientation.X, chartBase.Model));
                markerValue = clickTime.Ticks;
                showMarker = true;

                DrawMarker();
            }
            else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                CreateChartContextMenu();
            }
        }

        /// <summary>
        /// Called when a user moves the mouse over any of the charts.</summary>
        public void StripChart_MouseMoved(object sender, System.Windows.Input.MouseEventArgs e)
        {
            LiveCharts.Wpf.CartesianChart chartBase = (LiveCharts.Wpf.CartesianChart)sender;
            if (StripChart0.Base == chartBase) activeChart = StripChart0;
            else if (StripChart1.Base == chartBase) activeChart = StripChart1;
            else if (StripChart2.Base == chartBase) activeChart = StripChart2;
            else if (StripChart3.Base == chartBase) activeChart = StripChart3;
            mouseX = e.GetPosition(chartBase).X;
            mouseY = e.GetPosition(chartBase).Y;
            mouseTime = new DateTime((long)LiveCharts.ChartFunctions.FromPlotArea(mouseX, AxisOrientation.X, chartBase.Model));
            MouseTimeToolStripLabel.Text = "Mouse Location: " + mouseTime.ToString("MMM dd, yyyy  HH:mm:ss");
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
            SiteManagerForm siteManForm = new SiteManagerForm(this, siteMan);
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
                SitesTreeView.Nodes.Find(inst.GetName(), true)[0].Checked = true;
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
                SitesTreeView.Nodes.Find(eventGenerator.GetName(), true)[0].Checked = true;
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
            foreach (Instrument inst in activeInstruments)
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
            GenerateEvents(globalStart, globalEnd);
        }

        private void GenerateEvents(DateTime start, DateTime end)
        {
            List<EventGenerator> eventGenerators = new List<EventGenerator>();
            events = new List<Event>();

            //  Put all of the checked systems in the SitesTreeView in eventWatchers
            foreach (TreeNode siteNode in SitesTreeView.Nodes)
            {
                foreach (TreeNode facNode in siteNode.Nodes)
                {
                    foreach(TreeNode sysNode in facNode.Nodes)
                    {
                        foreach(TreeNode node in sysNode.Nodes)
                        {
                            if (node.Tag is EventGenerator && node.Checked)
                            {
                                eventGenerators.Add((EventGenerator)node.Tag);
                            }
                        }
                    }
                }
            }

            foreach(EventGenerator eg in eventGenerators)
            {
                events.AddRange(eg.GenerateEvents(start, end));
                eg.RunActions();
            }
            events.Sort((x, y) => x.GetStartTime().CompareTo(y.GetStartTime()));

            EventGridView.Rows.Clear();
            for (int i=0; i < events.Count(); i++)
            {
                EventGridView.Rows.Add(
                    events[i].GetEventGenerator().GetName(),
                    events[i].GetStartTime().ToString("MM/dd/yy HH:mm:ss"),
                    events[i].GetEndTime().ToString("MM/dd/yy HH:mm:ss"),
                    events[i].GetDuration().TotalSeconds,
                    events[i].GetMaxValue(),
                    events[i].GetMaxTime().ToString("MM/dd/yy HH:mm:ss"),
                    events[i].GetComment());
                EventGridView.Rows[i].Tag = events[i];
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
            EventManagerForm eventManForm = new EventManagerForm(this, siteMan);
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
            Cursor.Current = Cursors.WaitCursor;
            // File for each active channel
            foreach (ChannelPanel chanPan in chPanels)
            {
                bool plotChan = false;
                for (int chartNum = 0; chartNum < 4; chartNum++)
                {
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
                }
                if (plotChan)
                {
                    Channel chan = chanPan.GetChannel();

                    List<DateTime> dates = chan.GetTimeStamps();
                    List<double> vals = chan.GetValues();

                    StreamWriter file = new StreamWriter(chan.GetName() + ".csv");

                    for (int i = 0; i < dates.Count; i++)
                    {
                        file.WriteLine(dates[i].ToString() + "," + vals[i].ToString());
                    }
                    file.Close();
                }
            }

            // File for each active instrument
            foreach (Instrument inst in activeInstruments)
            {
                StreamWriter file = new StreamWriter(inst.GetName() + ".csv");
                List<DateTime> dates = inst.GetChannels()[0].GetTimeStamps();
                Channel[] channels = inst.GetChannels();
                for (int i = 0; i < dates.Count; i++)
                {
                    file.Write(dates[i].ToString("yyyy-MM-dd HH:mm:ss"));
                    for (int c = 0; c < channels.Length; c++)
                    {
                        file.Write("," + channels[c].GetValues()[i]);
                    }
                    file.Write("\r\n");
                }
                file.Close();
            }
            Cursor.Current = Cursors.Default;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox(VERSION);
            aboutBox.ShowDialog();
        }
    }
}
