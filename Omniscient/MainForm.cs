﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.WinForms;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Configurations;

using Omniscient.Parsers;
using Omniscient.Instruments;

namespace Omniscient
{
    /// <summary>
    /// MainForm is the window that appears when opening Omniscient as is where most of the user-interaction occurs.
    /// </summary>
    public partial class MainForm : Form
    {
        ///////////////////////////////////////////////////////////////////////
        private const int N_CHARTS = 4;
        public SiteManager siteMan;
        List<ChannelPanel> chPanels;
        List<Instrument> activeInstruments;

        private DateTime globalStart;
        private DateTime globalEnd;

        private bool rangeChanged = false;
        private bool bootingUp = false;

        private bool[] logScale;

        // The following are used to show a time marker when a user clicks a chart
        double mouseX = 0;
        private bool showMarker = false;
        private double markerValue = 0;
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// MainForm constructor
        /// </summary>
        public MainForm()
        {
            logScale = new bool[N_CHARTS];
            for (int c = 0; c < N_CHARTS; c++) logScale[c] = false;
            activeInstruments = new List<Instrument>();
            InitializeComponent();
        }

        /// <summary>
        /// MainForm_Load initializes several variables after the form is constructed.</summary>
        /// <remarks>Event hanlders for mouse events on the charts are handled 
        /// here because they cause errors when put in the form designer. </remarks>
        private void MainForm_Load(object sender, EventArgs e)
        {
            bootingUp = true;
            this.StripChart0.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart1.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart2.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart3.Base.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.StripChart_MouseClick);
            this.StripChart0.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);
            this.StripChart1.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);
            this.StripChart2.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);
            this.StripChart3.Base.MouseMove += new System.Windows.Input.MouseEventHandler(this.StripChart_MouseMoved);

            globalStart = DateTime.Today.AddDays(-7);
            GlobalStartTextBox.Text = globalStart.ToString("MMM dd, yyyy");
            globalEnd = DateTime.Today;
            GlobalEndTextBox.Text = globalEnd.ToString("MMM dd, yyyy");
            StartDatePicker.Value = globalStart;
            EndDatePicker.Value = globalEnd;
            StartTimePicker.Value = globalStart.Date;
            EndTimePicker.Value = globalEnd.Date;
            chPanels = new List<ChannelPanel>();
            siteMan = new SiteManager("SiteManager.xml");
            siteMan.Reload();
            UpdateSitesTree();
            RangeTextBox.Text = "7";
            RangeComboBox.Text = "Days";
            InitializeCharts();
            bootingUp = false;
            UpdateRange();
        }

        public void ClearPanels()
        {
            while (activeInstruments.Count>0)
            {
                RemoveChannelPanels(activeInstruments[0]);
            }
        }

        /// <summary>
        /// UpdateSitesTree() builds the tree view of the SiteManager.</summary>
        public void UpdateSitesTree()
        {
            SitesTreeView.Nodes.Clear();
            foreach(Site site in siteMan.GetSites())
            {
                TreeNode siteNode = new TreeNode(site.GetName());
                foreach(Facility fac in site.GetFacilities())
                {
                    TreeNode facNode = new TreeNode(fac.GetName());
                    foreach (DetectionSystem sys in fac.GetSystems())
                    {
                        TreeNode sysNode = new TreeNode(sys.GetName());
                        foreach (Instrument inst in sys.GetInstruments())
                        {
                            TreeNode instNode = new TreeNode(inst.GetName());
                            sysNode.Nodes.Add(instNode);
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
                    break;
                case 1:
                    return StripChart1;
                    break;
                case 2:
                    return StripChart2;
                    break;
                default:
                    return StripChart3;
                    break;
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

        /// <summary>
        /// UpdateChart is called whenever the data to be displayed on a chart changes. </summary>
        private void UpdateChart(int chartNum)
        {
            Cursor.Current = Cursors.WaitCursor;
            LiveCharts.WinForms.CartesianChart chart;
            chart = GetChart(chartNum);

            // Needed for speedy loading
            DateTime start = StartDatePicker.Value.Date;
            start = start.Add(StartTimePicker.Value.TimeOfDay);
            DateTime end = EndDatePicker.Value.Date;
            end = end.Add(EndTimePicker.Value.TimeOfDay);

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

                    // Load up the chart values
                    GearedValues<DateTimePoint> chartVals = new GearedValues<DateTimePoint>();
                    List<DateTimePoint> list = new List<DateTimePoint>();
                    if (RangeOnlyCheckBox.Checked)
                    {
                        if (logScale[chartNum])
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (vals[i] > 0 && dates[i] >= start  && dates[i] <= end)
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
                    }
                    else
                    {
                        if (logScale[chartNum])
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                if (vals[i] > 0)
                                    list.Add(new DateTimePoint(dates[i], vals[i]));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < dates.Count; ++i) //
                            {
                                list.Add(new DateTimePoint(dates[i], vals[i]));
                            }
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
            chart.Series = seriesColl;
            Cursor.Current = Cursors.Default;
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
                if (TimeSpan.FromTicks(end.Ticks - start.Ticks).TotalDays > 7)
                {
                    start = end.AddDays(-7);
                    RangeTextBox.Text = "7";
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

        /// <summary>
        /// Determines which instrument was selected or deselected and
        /// either adds or removes channel panels as appropriate.</summary>
        private void SitesTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            const int INST_LEVEL = 3;
            if ( e.Node.Level == INST_LEVEL)
            {
                string instName = e.Node.Text;
                string sysName = e.Node.Parent.Text;
                string facName = e.Node.Parent.Parent.Text;
                string siteName = e.Node.Parent.Parent.Parent.Text;
                foreach (Site site in siteMan.GetSites())
                {
                    if (site.GetName().Equals(siteName))
                    {
                        foreach (Facility fac in site.GetFacilities())
                        {
                            if (fac.GetName().Equals(facName))
                            {
                                foreach (DetectionSystem sys in fac.GetSystems())
                                {
                                    if (sys.GetName().Equals(sysName))
                                    {
                                        foreach (Instrument inst in sys.GetInstruments())
                                        {
                                            if(inst.GetName().Equals(instName))
                                            {
                                                if (e.Node.Checked)
                                                    AddChannelPanels(inst);
                                                else
                                                    RemoveChannelPanels(inst);
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }

                }
            }
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
                default:
                    newEnd = StartDatePicker.Value.AddMinutes(range);
                    break;
            }
            newEnd = newEnd.AddTicks(StartTimePicker.Value.TimeOfDay.Ticks);
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
            DateTime start = StartDatePicker.Value.Date;
            start = start.Add(StartTimePicker.Value.TimeOfDay);
            DateTime end = EndDatePicker.Value.Date;
            end = end.Add(EndTimePicker.Value.TimeOfDay);

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
            if (RangeOnlyCheckBox.Checked)
                UpdatesCharts();
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
            StartDatePicker.Value = newStart;
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
            for (int i = 0; i < N_CHARTS; ++i)
            {
                LiveCharts.WinForms.CartesianChart chart;
                switch (i)
                {
                    case 0:
                        chart = StripChart0;
                        break;
                    case 1:
                        chart = StripChart1;
                        break;
                    case 2:
                        chart = StripChart2;
                        break;
                    default:
                        chart = StripChart3;
                        break;
                }

                chart.AxisX[0].Sections.Clear();
                chart.AxisX[0].Sections = new SectionsCollection()
                {
                    new AxisSection()
                    {
                        Value = markerValue,
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                        StrokeThickness = 1,
                    }
                };
                chart.Update(true, true);
            }
            DateTime markerTime = new DateTime((long)markerValue);
            MarkerToolStripLabel.Text = "Marker Location: " + markerTime.ToString("MMM dd, yyyy  HH:mm:ss");
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Called when a user clicks on any of the charts.</summary>
        private void StripChart_MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LiveCharts.Wpf.CartesianChart chartBase = (LiveCharts.Wpf.CartesianChart)sender;
            DateTime clickTime = new DateTime((long)LiveCharts.ChartFunctions.FromPlotArea(mouseX, AxisOrientation.X, chartBase.Model));
            markerValue = clickTime.Ticks;
            showMarker = true;

            DrawMarker();
        }

        /// <summary>
        /// Called when a user moves the mouse over any of the charts.</summary>
        public void StripChart_MouseMoved(object sender, System.Windows.Input.MouseEventArgs e)
        {
            mouseX = e.GetPosition((LiveCharts.Wpf.CartesianChart) sender).X;
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

        private void RangeOnlyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!RangeOnlyCheckBox.Checked)
                UpdatesCharts();
        }

        private void SiteManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SiteManagerForm siteManForm = new SiteManagerForm(this, siteMan);
            siteManForm.Show();
        }
    }
}
