using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Converters;
using System.Threading;
using System.IO;
using System.Data;
using System.Media;

using NationalInstruments;
using NationalInstruments.DAQmx;
using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;

// v0.1
// v0.2
// v0.3
// v0.4
// v0.5
// v0.6
// v0.7
// v0.8
// v0.9
// v1.0
// v1.1: 
// v1.2: added extra progress boxes, changed reagent bottles to new configuration
// v1.3
// v1.4
// v1.5
// v1.6
// v1.7
// v1.8

namespace AutoVega4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isReading = false;
        readonly string logFilePath;
        readonly string outputFilePath;
        readonly string dataFilePath;
        readonly string timeStamp;
        readonly string testTime;
        readonly string writeAllSteps;
        readonly string readLimSwitches;
        readonly string switchBanks;
        readonly string delimiter;
        readonly string[] map;
        readonly string[] shiftAndScale;
        string testResult;
        string sampleName;
        string outputFileData;
        int incubationMinutes1;
        int incubationMinutes2;
        double drainMinutes;
        int drainTime;
        readonly int wait = 0;
        double raw_avg;
        double TC_rdg;
        double diff;

        [DllImport(@".\DLLs\DAQinterfaceForKaya17.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr verifyInput(double[] input);

        [DllImport(@".\DLLs\DAQinterfaceForKaya17.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern bool testSetSettings(double[] input);

        [DllImport(@".\DLLs\DAQinterfaceForKaya17.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern bool testInitializeBoard(StringBuilder str, int len);

        [DllImport(@".\DLLs\DAQinterfaceForKaya17.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr testGetBoardValue(StringBuilder str, int len, double excVoltage);

        [DllImport(@".\DLLs\DAQinterfaceForKaya17.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void testCloseTasksAndChannels();

        public MainWindow()
        {
            Directory.CreateDirectory(@"C:\Users\Public\Documents\kaya17\log");
            Directory.CreateDirectory(@"C:\Users\Public\Documents\kaya17\data");

            timeStamp = DateTime.Now.ToString("ddMMMyy_HHmmss");
            testTime = DateTime.Now.ToString("ddMMM_HHmm");

            logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega4_logfile.txt";
            outputFilePath = @"C:\Users\Public\Documents\Kaya17\Data\kaya17-AutoVega4_" + timeStamp + "output.csv";
            dataFilePath = @"C:\Users\Public\Documents\Kaya17\Data\kaya17-AutoVega4_data.csv";

            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            if (File.Exists(dataFilePath))
            {
                File.Delete(dataFilePath);
            }

            writeAllSteps = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];
            readLimSwitches = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[1];
            switchBanks = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[2];

            map = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\34_well_cartridge_steps_1row_test.csv");

            shiftAndScale = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\Kaya17_32Well_Shift_Scale.csv");

            delimiter = ",";

            isReading = false;

            testResult = "NEG";

            InitializeComponent();
        }

        private void operator_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if (operator_tb.Text == "")
            //{
            //    ImageBrush textImageBrush = new ImageBrush();
            //    textImageBrush.ImageSource =
            //        new BitmapImage(
            //            new Uri(@"TextBoxBackground.gif", UriKind.Relative)
            //        );
            //    textImageBrush.AlignmentX = AlignmentX.Left;
            //    textImageBrush.Stretch = Stretch.None;
            //    operator_tb.Background = textImageBrush;
            //}
            //else
            //{
            //    operator_tb.Background = null;
            //}

            try
            {
                if (kit_tb != null && kit_tb.IsEnabled == false)
                {
                    operator_tb.BorderBrush = Brushes.White;
                    kit_tb.IsEnabled = true;
                    kit_tb.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFC300");
                }
            }
            catch { }

            File.AppendAllText(logFilePath, "Kit id enabled" + Environment.NewLine);
        }

        private void kit_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (reader_tb != null && reader_tb.IsEnabled == false)
                {
                    kit_tb.BorderBrush = Brushes.White;
                    reader_tb.IsEnabled = true;
                    reader_tb.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFC300");
                }
            }
            catch { }

            File.AppendAllText(logFilePath, "Reader id enabled" + Environment.NewLine);
        }

        private void reader_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                reader_tb.BorderBrush = Brushes.White;

                if (PIGF_rb != null && PIGF_rb.IsEnabled == false)
                {
                    PIGF_rb.IsEnabled = true;
                }

                if (CRP_rb != null && CRP_rb.IsEnabled == false)
                {
                    CRP_rb.IsEnabled = true;
                }

                if (IL6_rb != null && IL6_rb.IsEnabled == false)
                {
                    IL6_rb.IsEnabled = true;
                }

                if (Ovarian_rb != null && Ovarian_rb.IsEnabled == false)
                {
                    Ovarian_rb.IsEnabled = true;
                }
            }
            catch { }

            reader_tb.Text = shiftAndScale[0].Split(',')[1];

            testname_tb.Text = "Kaya17-AutoVega_" + testTime;
            testname_tb.FontSize = 16;

            File.AppendAllText(logFilePath, "Test type radiobuttons enabled" + Environment.NewLine);
        }        

        private async void start_button_Click(object sender, RoutedEventArgs e)
        {
            string appendOutputHeaders = "Operator ID" + delimiter + "Kit ID" + delimiter +
                "Reader ID" + delimiter + "Test Type" + delimiter + "Test Date" + delimiter + "Test Time" + delimiter + Environment.NewLine;
            try
            {
                File.WriteAllText(outputFilePath, appendOutputHeaders);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            File.AppendAllText(logFilePath, "File headers written" + Environment.NewLine);

            string operator_ID = operator_tb.Text;
            string kit_ID = kit_tb.Text;
            string reader_ID = reader_tb.Text;

            File.AppendAllText(logFilePath, "IDs set" + Environment.NewLine);

            if ((bool)SFLT_rb.IsChecked == true)
            {
                string test = "SFLT";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + DateTime.Now.ToString("MM/dd/yyyy") +
                delimiter + DateTime.Now.ToString("hh:mm tt") + delimiter + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)PIGF_rb.IsChecked == true)
            {
                string test = "PIGF";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + DateTime.Now.ToString("MM/dd/yyyy") +
                delimiter + DateTime.Now.ToString("hh:mm tt") + delimiter + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)CRP_rb.IsChecked == true)
            {
                string test = "CRP";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + DateTime.Now.ToString("MM/dd/yyyy") +
                delimiter + DateTime.Now.ToString("hh:mm tt") + delimiter + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)IL6_rb.IsChecked == true)
            {
                string test = "IL6";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + DateTime.Now.ToString("MM/dd/yyyy") +
                delimiter + DateTime.Now.ToString("hh:mm tt") + delimiter + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)Ovarian_rb.IsChecked == true)
            {
                string test = "Ovarian Panel";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + DateTime.Now.ToString("MM/dd/yyyy") +
                delimiter + DateTime.Now.ToString("hh:mm tt") + delimiter + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }

            string[] positions = new string[map.Length];
            int[] xPos = new int[map.Length];
            int[] yPos = new int[map.Length];
            int[] zPos = new int[map.Length];

            for (int i = 0; i < map.Length; i++)
            {
                positions[i] = map[i].Split(',')[0];
                xPos[i] = Int32.Parse(map[i].Split(',')[1]);
                yPos[i] = Int32.Parse(map[i].Split(',')[2]);
                zPos[i] = Int32.Parse(map[i].Split(',')[3]);
            }

            AutoClosingMessageBox.Show("Moving to home position", "Home", 2000);
            File.AppendAllText(logFilePath, "Moving to home position" + Environment.NewLine);

            MoveToHomePosition();

            AutoClosingMessageBox.Show("Cartridge at home position", "Home", 2000);
            File.AppendAllText(logFilePath, "Cartridge at home position" + Environment.NewLine);

            AutoClosingMessageBox.Show("Moving to load position", "Load", 2000);
            File.AppendAllText(logFilePath, "Moving to load position" + Environment.NewLine);

            MoveToLoadPosition();

            AutoClosingMessageBox.Show("Cartridge at load position", "Load", 2000);
            File.AppendAllText(logFilePath, "Cartridge at load position" + Environment.NewLine);

            // **TODO: Check for cartridge alignment**

            // enable patient info, sample number, and read button
            sampleNum_tb.IsEnabled = true;
            read_button.IsEnabled = true;

            File.AppendAllText(logFilePath, "Patient info, sample number, and read button enabled" + Environment.NewLine);

            // disable operator, kit, reader, radiobuttons
            operator_tb.IsEnabled = false;
            kit_tb.IsEnabled = false;
            reader_tb.IsEnabled = false;
            SFLT_rb.IsEnabled = false;
            PIGF_rb.IsEnabled = false;
            CRP_rb.IsEnabled = false;
            IL6_rb.IsEnabled = false;
            Ovarian_rb.IsEnabled = false;

            File.AppendAllText(logFilePath, "Operator id, kit id, reader id, and test type radiobuttons disabled" + Environment.NewLine);

            MessageBox.Show("Please click on each well and type sample name or scan QR code on sample tube. " +
                "Once all sample names are entered and incubation and drain times are set, press Start Test.");
        }

        enum steppingPositions
        {
            Home = 0,
            Back_Off = 1,
            Home_to_Load = 2,
            Load = 3,
            Drain = 4,
            Probe_Bottle = 5,
            RB_Bottle = 6,
            HBSS_Bottle = 7,
            Wash_Bottle = 8,
            Dispense_to_Read = 9,
            A1 = 10,
            B1 = 11,
            C1 = 12,
            D1 = 13,
            E2 = 14,
            D2 = 15,
            C2 = 16,
            B2 = 17,
            A2 = 18,
            A3 = 19,
            B3 = 20,
            C3 = 21,
            D3 = 22,
            E3 = 23,
            E4 = 24,
            D4 = 25,
            C4 = 26,
            B4 = 27,
            A4 = 28,
            A5 = 29,
            B5 = 30,
            C5 = 31,
            D5 = 32,
            E5 = 33,
            E6 = 34,
            D6 = 35,
            C6 = 36,
            B6 = 37,
            A6 = 38,
            A7 = 39,
            B7 = 40,
            C7 = 41,
            D7 = 42,
            E7 = 43,
        }

        enum shiftsAndScales
        {
            A1 = 1,
            B1 = 2,
            C1 = 3,
            D1 = 4,
            E2 = 5,
            D2 = 6,
            C2 = 7,
            B2 = 8,
            A2 = 9,
            A3 = 10,
            B3 = 11,
            C3 = 12,
            D3 = 13,
            E3 = 14,
            E4 = 15,
            D4 = 16,
            C4 = 17,
            B4 = 18,
            A4 = 19,
            A5 = 20,
            B5 = 21,
            C5 = 22,
            D5 = 23,
            E5 = 24,
            E6 = 25,
            D6 = 26,
            C6 = 27,
            B6 = 28,
            A6 = 29,
            A7 = 30,
            B7 = 31,
            C7 = 32,
            D7 = 33,
            E7 = 34,
        }

        private void MoveToHomePosition()
        {
            string[] positions = new string[map.Length];
            int[] xPos = new int[map.Length];
            int[] yPos = new int[map.Length];
            int[] zPos = new int[map.Length];

            for (int i = 0; i < map.Length; i++)
            {
                positions[i] = map[i].Split(',')[0];
                xPos[i] = Int32.Parse(map[i].Split(',')[1]);
                yPos[i] = Int32.Parse(map[i].Split(',')[2]);
                zPos[i] = Int32.Parse(map[i].Split(',')[3]);
            }

            // move to z limit switch and back off a certain number of steps
            for (int i = 0; i < zPos[(int)steppingPositions.Home]; i++)
            {
                // move z up
                using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                {
                    //  Create an Digital Output channel and name it.
                    digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                        ChannelLineGrouping.OneChannelForAllLines);

                    //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                    //  of digital data on demand, so no timeout is necessary.
                    DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                    writer.WriteSingleSamplePort(true, 192);
                    Thread.Sleep(wait);
                    writer.WriteSingleSamplePort(true, 128);
                }

                // check for the z limit switch
                using (NationalInstruments.DAQmx.Task digitalReadTask = new NationalInstruments.DAQmx.Task())
                {
                    digitalReadTask.DIChannels.CreateChannel(
                        readLimSwitches,
                        "port1",
                        ChannelLineGrouping.OneChannelForAllLines);

                    DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                    UInt32 data = reader.ReadSingleSamplePortUInt32();

                    //Update the Data Read box
                    string LimitInputText = data.ToString();

                    if (LimitInputText == "4" || LimitInputText == "5" || LimitInputText == "6" || LimitInputText == "7")
                    {
                        // steps z back so limit switch is not pressed
                        for (int j = 0; j < zPos[(int)steppingPositions.Back_Off]; j++)
                        {
                            // if the z limit switch is reached, move z down a certain number of steps
                            using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                            {
                                //  Create an Digital Output channel and name it.
                                digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                    ChannelLineGrouping.OneChannelForAllLines);

                                //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                                //  of digital data on demand, so no timeout is necessary.
                                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                                writer.WriteSingleSamplePort(true, 64);
                                Thread.Sleep(wait);
                                writer.WriteSingleSamplePort(true, 0);
                            }
                        }
                        // stop moving z
                        break;
                    }
                }
            }

            // move to x limit switch and back off a certain number of steps
            for (int i = 0; i < xPos[(int)steppingPositions.Home]; i++)
            {
                // move x negative
                using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                {
                    //  Create an Digital Output channel and name it.
                    digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                        ChannelLineGrouping.OneChannelForAllLines);

                    //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                    //  of digital data on demand, so no timeout is necessary.
                    DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                    writer.WriteSingleSamplePort(true, 16);
                    Thread.Sleep(wait);
                    writer.WriteSingleSamplePort(true, 0);
                }

                // check for the x limit switch
                using (NationalInstruments.DAQmx.Task digitalReadTask = new NationalInstruments.DAQmx.Task())
                {
                    digitalReadTask.DIChannels.CreateChannel(
                        readLimSwitches,
                        "port1",
                        ChannelLineGrouping.OneChannelForAllLines);

                    DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                    UInt32 data = reader.ReadSingleSamplePortUInt32();

                    //Update the Data Read box
                    string LimitInputText = data.ToString();

                    if (LimitInputText == "1" || LimitInputText == "3" || LimitInputText == "5" || LimitInputText == "7")
                    {
                        for (int j = 0; j < xPos[(int)steppingPositions.Back_Off]; j++)
                        {
                            // if the x limit switch is reached, move x forward a certain number of steps
                            using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                            {
                                //  Create an Digital Output channel and name it.
                                digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                    ChannelLineGrouping.OneChannelForAllLines);

                                //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                                //  of digital data on demand, so no timeout is necessary.
                                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                                writer.WriteSingleSamplePort(true, 32);
                                //Thread.Sleep(wait);
                                writer.WriteSingleSamplePort(true, 48);
                            }
                        }
                        // stop moving x
                        break;
                    }
                }
            }

            // move to y limit switch and back off a certain number of steps
            for (int i = 0; i < yPos[(int)steppingPositions.Home]; i++)
            {
                // move y negative
                using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                {
                    //  Create an Digital Output channel and name it.
                    digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                        ChannelLineGrouping.OneChannelForAllLines);

                    //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                    //  of digital data on demand, so no timeout is necessary.
                    DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                    writer.WriteSingleSamplePort(true, 4);
                    Thread.Sleep(wait);
                    writer.WriteSingleSamplePort(true, 0);
                }

                // check for the y limit switch
                using (NationalInstruments.DAQmx.Task digitalReadTask = new NationalInstruments.DAQmx.Task())
                {
                    digitalReadTask.DIChannels.CreateChannel(
                        readLimSwitches,
                        "port1",
                        ChannelLineGrouping.OneChannelForAllLines);

                    DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                    UInt32 data = reader.ReadSingleSamplePortUInt32();

                    //Update the Data Read box
                    string LimitInputText = data.ToString();

                    if (LimitInputText == "2" || LimitInputText == "3" || LimitInputText == "6" || LimitInputText == "7")
                    {
                        // if the y limit switch is reached, move y forward a certain number of steps
                        for (int j = 0; j < yPos[(int)steppingPositions.Back_Off]; j++)
                        {
                            using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                            {
                                //  Create an Digital Output channel and name it.
                                digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                    ChannelLineGrouping.OneChannelForAllLines);

                                //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                                //  of digital data on demand, so no timeout is necessary.
                                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                                writer.WriteSingleSamplePort(true, 12);
                                Thread.Sleep(wait);
                                writer.WriteSingleSamplePort(true, 8);
                            }
                        }
                        // stop moving y
                        break;
                    }
                }
            }
        }

        private void MoveToLoadPosition()
        {
            string[] positions = new string[map.Length];
            int[] xPos = new int[map.Length];
            int[] yPos = new int[map.Length];
            int[] zPos = new int[map.Length];

            for (int i = 0; i < map.Length; i++)
            {
                positions[i] = map[i].Split(',')[0];
                xPos[i] = Int32.Parse(map[i].Split(',')[1]);
                yPos[i] = Int32.Parse(map[i].Split(',')[2]);
                zPos[i] = Int32.Parse(map[i].Split(',')[3]);
            }

            // turns motor y positive
            for (int i = 0; i < yPos[(int)steppingPositions.Home_to_Load]; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 12);
                        Thread.Sleep(wait);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void read_button_Click(object sender, RoutedEventArgs e)
        {
            string inProgressColor = "#F5D5CB";
            string finishedColor = "#D7ECD9";

            isReading = true;
            File.AppendAllText(logFilePath, "Reading started" + Environment.NewLine);
            List<TestResults> testResults = new List<TestResults>();

            // define array for necessary items from parameter file
            double[] testArray = new double[17];

            // Hide Cartridge Display
            resultsDisplay_cart34_border.Visibility = Visibility.Hidden;
            resultsDisplay_cart34.Visibility = Visibility.Hidden;

            // Show In Progress Boxes
            inProgressBG_r.Visibility = Visibility.Visible;
            inProgress_stack.Visibility = Visibility.Visible;
            inProgress_cart34.Visibility = Visibility.Visible;
            inProgress_cart34_border.Visibility = Visibility.Visible;

            Ellipse[] inProgressEllipses = {inProgressA1,inProgressB1,inProgressC1,inProgressD1,                /*Row 1*/
                                                inProgressE2,inProgressD2,inProgressC2,inProgressB2,inProgressA2,   /*Row 2*/
                                                inProgressA3,inProgressB3,inProgressC3,inProgressD3,inProgressE3,   /*Row 3*/
                                                inProgressE4,inProgressD4,inProgressC4,inProgressB4,inProgressA4,   /*Row 4*/
                                                inProgressA5,inProgressB5,inProgressC5,inProgressD5,inProgressE5,   /*Row 5*/
                                                inProgressE6,inProgressD6,inProgressC6,inProgressB6,inProgressA6,   /*Row 6*/
                                                inProgressA7,inProgressB7,inProgressC7,inProgressD7,inProgressE7};  /*Row 7*/

            TextBox[] resultsTextboxes = {resultsDisplayA1,resultsDisplayB1,resultsDisplayC1,resultsDisplayD1,                    /*Row 1*/ /*0-3*/
                                              resultsDisplayE2,resultsDisplayD2,resultsDisplayC2,resultsDisplayB2,resultsDisplayA2,   /*Row 2*/ /*4-8*/
                                              resultsDisplayA3,resultsDisplayB3,resultsDisplayC3,resultsDisplayD3,resultsDisplayE3,   /*Row 3*/ /*9-13*/
                                              resultsDisplayE4,resultsDisplayD4,resultsDisplayC4,resultsDisplayB4,resultsDisplayA4,   /*Row 4*/ /*14-18*/
                                              resultsDisplayA5,resultsDisplayB5,resultsDisplayC5,resultsDisplayD5,resultsDisplayE5,   /*Row 5*/ /*19-23*/
                                              resultsDisplayE6,resultsDisplayD6,resultsDisplayC6,resultsDisplayB6,resultsDisplayA6,   /*Row 6*/ /*24-28*/
                                              resultsDisplayA7,resultsDisplayB7,resultsDisplayC7,resultsDisplayD7,resultsDisplayE7};  /*Row 7*/ /*29-33*/

            File.AppendAllText(logFilePath, "In progress information visible" + Environment.NewLine);

            // read parameter file and read in all necessary parameters
            string[] parameters = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\Kaya17Covi2V.txt");
            double ledOutputRange = double.Parse(parameters[0].Substring(0, 3));
            double numSamplesPerReading = double.Parse(parameters[1].Substring(0, 3));
            double numTempSamplesPerReading = double.Parse(parameters[2].Substring(0, 2));
            double samplingRate = double.Parse(parameters[3].Substring(0, 5));
            double numSamplesForAvg = double.Parse(parameters[4].Substring(0, 3));
            double errorLimitInMillivolts = double.Parse(parameters[5].Substring(0, 2));
            double saturation = double.Parse(parameters[8].Substring(0, 8));
            double expectedDarkRdg = double.Parse(parameters[6].Substring(0, 4));
            double lowSignal = double.Parse(parameters[29].Substring(0, 5));
            double readMethod = double.Parse(parameters[9].Substring(0, 1));
            double ledOnDuration = double.Parse(parameters[9].Substring(4, 3));
            double readDelayInMS = double.Parse(parameters[9].Substring(8, 1));
            double excitationLedVoltage = double.Parse(parameters[33].Substring(0, 5));
            double excMinVoltage = double.Parse(parameters[26].Substring(0, 3));
            double excNomVoltage = double.Parse(parameters[25].Substring(0, 4));
            double excMaxVoltage = double.Parse(parameters[24].Substring(0, 3));
            double calMinVoltage = double.Parse(parameters[18].Substring(0, 3));
            double calNomVoltage = double.Parse(parameters[17].Substring(0, 3));
            double calMaxVoltage = double.Parse(parameters[16].Substring(0, 3));
            double afeScaleFactor = double.Parse(parameters[35].Substring(0, 3));
            double afeShiftFactor = double.Parse(parameters[36].Substring(0, 4));
            double viralCountScaleFactor = double.Parse(parameters[45].Substring(0, 5));
            double viralCountOffsetFactor = double.Parse(parameters[46].Substring(0, 4));
            double antigenCutoffFactor = double.Parse(parameters[47].Substring(0, 1));
            double antigenNoiseMargin = double.Parse(parameters[48].Substring(0, 1));
            double antigenControlMargin = double.Parse(parameters[49].Substring(0, 2));

            double bgd_rdg = afeShiftFactor;
            double threshold = viralCountOffsetFactor;

            string appendInitVals = "Initialization Complete" + delimiter + "Bgd Rdg: " + bgd_rdg + delimiter + "Threshold: "
                                    + threshold + delimiter + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(outputFilePath, appendInitVals);

            File.AppendAllText(logFilePath, "Parameters read in" + Environment.NewLine);

            testArray[0] = ledOutputRange;
            testArray[1] = samplingRate;
            testArray[2] = numSamplesPerReading;
            testArray[3] = numSamplesForAvg;
            testArray[4] = errorLimitInMillivolts;
            testArray[5] = numTempSamplesPerReading;
            testArray[6] = saturation;
            testArray[7] = expectedDarkRdg;
            testArray[8] = lowSignal;
            testArray[9] = ledOnDuration;
            testArray[10] = readDelayInMS;
            testArray[11] = calMinVoltage;
            testArray[12] = calNomVoltage;
            testArray[13] = calMaxVoltage;
            testArray[14] = excMinVoltage;
            testArray[15] = excNomVoltage;
            testArray[16] = excMaxVoltage;

            File.AppendAllText(logFilePath, "Parameter array set" + Environment.NewLine);

            string inputString = "";
            foreach (double value in testArray)
            {
                inputString += "Input: " + value + "\n";
            }

            File.AppendAllText(logFilePath, inputString + Environment.NewLine);

            IntPtr testPtr = verifyInput(testArray);
            double[] testArray2 = new double[17];
            Marshal.Copy(testPtr, testArray2, 0, 17);
            inputString = "";
            foreach (double value in testArray)
            {
                inputString += "Verify input: " + value + "\n";
            }

            File.AppendAllText(logFilePath, inputString + Environment.NewLine);

            bool settingsBool = testSetSettings(testArray);

            File.AppendAllText(logFilePath, "Test setSettings: " + settingsBool + Environment.NewLine);

            string[] positions = new string[map.Length];
            int[] xPos = new int[map.Length];
            int[] yPos = new int[map.Length];
            int[] zPos = new int[map.Length];
            int[] pump = new int[map.Length];

            for (int i = 0; i < map.Length; i++)
            {
                positions[i] = map[i].Split(',')[0];
                xPos[i] = Int32.Parse(map[i].Split(',')[1]);
                yPos[i] = Int32.Parse(map[i].Split(',')[2]);
                zPos[i] = Int32.Parse(map[i].Split(',')[3]);
                pump[i] = Int32.Parse(map[i].Split(',')[4]);
            }

            double[] shiftFactors = new double[shiftAndScale.Length];
            double[] scaleFactors = new double[shiftAndScale.Length];

            // skip the first line for shift and scale factors
            for (int i = 1; i < shiftAndScale.Length; i++)
            {
                shiftFactors[i] = double.Parse(shiftAndScale[i].Split(',')[1]);
                scaleFactors[i] = double.Parse(shiftAndScale[i].Split(',')[2]);
            }

            // Enter '234' for all 15 wells
            if (sampleNum_tb.Text == "234")
            {
                // 540 steps = 1mL

                // ** Start of moving steps **
                // ---------------------------

                // ** HBSS Dispensing, Incubation, and Drain **
                // --------------------------------------------

                // ** HBSS Dispensing **
                // ---------------------

                //// Move to HBSS Bottle
                //moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Load]);
                //moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Load]);

                //// Draw HBSS for row 2
                //AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                //File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                //// Lower Z by 8000 steps
                //lowerZPosition(8000);

                //// Draw 820 steps (1.5mL + extra)
                //drawLiquid(820);

                //// Raise Z by 8000 steps
                //raiseZPosition(8000);

                //// Change HBSS Dispense Box to in progress color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Foreground = Brushes.Black;

                ////**E2**//
                //// Move from HBSS bottle to E2
                //moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.HBSS_Bottle]);
                //moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.HBSS_Bottle]);

                //// Change E2 to in progress color
                //inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //AutoClosingMessageBox.Show("Dispensing HBSS in E2", "Dispensing", 1000);

                //// Dispense 300ul HBSS in E2
                //dispenseLiquid(164);

                //// Change E2 to finished color
                //inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //// Dispense HBSS in remaining wells
                //for (int i = 15; i < 29; i++)
                //{
                //    // Move to next well
                //    moveY(yPos[i] - yPos[i - 1]);
                //    moveX(xPos[i] - xPos[i - 1]);

                //    // Change current well to in progress color
                //    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //    // Before dispensing in A3, go back to HBSS bottle and draw more
                //    if (i == 19)
                //    {
                //        // Move from A3 to HBSS bottle
                //        moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.A3]);
                //        moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.A3]);

                //        // Draw HBSS for rows 3 and 4
                //        AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                //        File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                //        // Lower z position by 9800 steps
                //        lowerZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                //        // Draw 1640 steps (3mL + extra)
                //        drawLiquid(1640);

                //        // Raise by 9800 steps
                //        raiseZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                //        // Move back to A3
                //        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.HBSS_Bottle]);
                //        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.HBSS_Bottle]);
                //    }

                //    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                //    // dispense remaining liquid in last well
                //    if (i == 18 || i == 28)
                //    {
                //        // Dispense 300ul HBSS + remaining
                //        dispenseLiquid(164);

                //        // wait 3 seconds and dispense remaining amount
                //        Task.Delay(3000).Wait();

                //        dispenseLiquid(50);
                //    }
                //    else
                //    {
                //        // Dispense 300ul HBSS
                //        dispenseLiquid(164);
                //    }

                //    // Change current well to finished color and next well to in progress color except for last time
                //    if (i == 28)
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //    }
                //    else
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //    }
                //}

                //// Change HBSS Dispense Box to Finished Color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                ////MessageBox.Show("HBSS Dispensed");
                //AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 1000);
                //File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                //// -----------------------------
                //// ** HBSS Dispense Complete **

                //// ** HBSS Wash **
                //// ---------------

                //// Change wells to gray
                //for (int i = 4; i < 19; i++)
                //{
                //    inProgressEllipses[i].Fill = Brushes.Gray;
                //}

                //AutoClosingMessageBox.Show("Cleaning Probe Tip", "Cleaning", 1000);

                //// Move from A4 to Wash_Bottle
                //moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                //moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                //// Lower pipette tips
                //lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Draw 2430 steps (4.5mL)
                //drawLiquid(2430);

                //// Raise pipette tips
                //raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Dispense 2500 steps (4.5mL + extra)
                //dispenseLiquid(2500);

                //Task.Delay(5000).Wait();

                //dispenseLiquid(300);

                //// ------------------------
                //// ** HBSS Wash Complete **

                // ** Sample Incubation and Draining **
                // ----------------------------------

                // Change Sample Incubation and Draining Box to in progress color
                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Foreground = Brushes.Black;

                // Change wells to in progress color
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes1 = Int32.Parse(incubationTime1_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes1 + " minutes", "Incubating", 1000);

                for (int i = 0; i < incubationMinutes1; i++)
                {
                    int remaining = incubationMinutes1 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Load]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Load]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                drainMinutes = double.Parse(drainTime_tb.Text);
                drainTime = Convert.ToInt32(drainMinutes * 60 * 1000);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for samples to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for samples to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Sample Draining Box to finished color
                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change wells to finished color
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // ----------------------------------------
                // ** Sample Incubation and Drain Complete **

                // -----------------------------------------------------
                // ** Sample Dispensing, Incubation, and Drain Complete **

                // ** Probe Dispensing **
                // ----------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Change Probe Dispense Box to in progress color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Foreground = Brushes.Black;

                // Change cartridge wells to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move to Probe Bottle
                moveX(xPos[(int)steppingPositions.Probe_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.Probe_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Draw Probe for row 2
                AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                // Lower Z slightly less than the full amount
                lowerZPosition(zPos[(int)steppingPositions.Probe_Bottle] - 1500);

                // Draw 918 steps (1.7mL)
                drawLiquid(918);

                // Raise Z
                raiseZPosition(zPos[(int)steppingPositions.Probe_Bottle] - 1500);

                //**E2**//
                // Move from Probe bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.Probe_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.Probe_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in E2", "Dispensing", 1000);

                // Dispense 300ul Probe in E2
                dispenseLiquid(162);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense Probe in remaining wells
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Before dispensing in A3, go back to Probe bottle and draw more
                    if (i == 19)
                    {
                        // Move from A3 to Probe bottle
                        moveX(xPos[(int)steppingPositions.Probe_Bottle] - xPos[(int)steppingPositions.A3]);
                        moveY(yPos[(int)steppingPositions.Probe_Bottle] - yPos[(int)steppingPositions.A3]);

                        // Dispense remaining liquid before drawing more
                        dispenseLiquid(200);

                        // Draw Probe for rows 3 and 4
                        AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                        File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                        // Lower z position by 9800 steps
                        lowerZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                        // Draw 1640 steps (3mL + extra)
                        drawLiquid(1640);

                        // Raise by 9800 steps
                        raiseZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                        // Move back to A3
                        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.Probe_Bottle]);
                        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.Probe_Bottle]);
                    }

                    AutoClosingMessageBox.Show("Dispensing Probe in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 28)
                    {
                        // Dispense 300ul Probe
                        dispenseLiquid(162);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(50);
                    }
                    else
                    {
                        // Dispense 300ul Probe
                        dispenseLiquid(162);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change probe dispense box to finished color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Probe Dispensed", "Dispensing Complete", 1000);
                File.AppendAllText(logFilePath, "Probe Dispensed" + Environment.NewLine);

                // -----------------------------
                // ** Probe Dispense Complete **

                // ** Probe Wash **
                // ----------------

                // Change wells to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Probe Tip", "Cleaning", 1000);

                // Move from A4 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** Probe Wash Complete **

                // ** Probe Incubation and Drain **
                // --------------------------------

                // Change Probe Draining Box and Cartridge to in progress color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Foreground = Brushes.Black;
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes2 = Int32.Parse(incubationTime2_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes2 + " minutes", "Incubating", 3000);

                for (int i = 0; i < incubationMinutes2; i++)
                {
                    int remaining = incubationMinutes2 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for probe to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for probe to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult2 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult2 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult2 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Probe Draining Box and Cartridge to finished color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -----------------------------------------
                // ** Probe Incubation and Drain Complete **

                // ** HBSS Dispense **
                // -------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to HBSS_Bottle
                moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Draw 1188 steps (2.2mL)
                drawLiquid(1188);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Change HBSS Dispense Box to in progress color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing HBSS", "Dispensing", 1000);

                //**E2**//
                // Move from HBSS bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.HBSS_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.HBSS_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing HBSS in E2", "Dispensing", 1000);

                // Dispense 400ul HBSS in E2
                dispenseLiquid(216);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense HBSS in remaining wells
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Before dispensing in A3, go back to HBSS bottle and draw more
                    if (i == 19)
                    {
                        // Move from A3 to HBSS bottle
                        moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.A3]);
                        moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.A3]);

                        // Dispense remaining liquid before drawing more
                        dispenseLiquid(200);

                        // Draw HBSS for rows 3 and 4
                        AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                        File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                        // Lower z position by 9800 steps
                        lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Draw 2200 steps (4mL + extra)
                        drawLiquid(2200);

                        // Raise by 9800 steps
                        raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Move back to A3
                        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.HBSS_Bottle]);
                        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.HBSS_Bottle]);
                    }

                    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 28)
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(100);
                    }
                    else
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change HBSS Dispense box to finished color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                // ----------------------------
                // ** HBSS Dispense Complete **

                // ** HBSS Wash **
                // ----------------

                // Change wells to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                // Move from A4 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** HBSS Wash Complete **

                // ** HBSS Drain **
                // ----------------

                // Change HBSS Draining Box and Cartridge to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for HBSS to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for HBSS to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult3 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult3 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult3 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change HBSS Draining Box and Cartridge to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -------------------------
                // ** HBSS Drain Complete **

                // ** RB Dispense **
                // -----------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Draw 1242 steps (2.3mL)
                drawLiquid(1242);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Change RB Dispense Box to in progress color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing RB", "Dispensing", 1000);

                //**E2**//
                // Move from RB bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.RB_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.RB_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in E2", "Dispensing", 1000);

                // Dispense 420ul RB in E2
                dispenseLiquid(227);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense RB in remaining wells
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Before dispensing in A3, go back to RB bottle and draw more
                    if (i == 19)
                    {
                        // Move from A3 to RB bottle
                        moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.A3]);
                        moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.A3]);

                        // Dispense remaining liquid before drawing more
                        dispenseLiquid(200);

                        // Draw RB for rows 3 and 4
                        AutoClosingMessageBox.Show("Drawing RB", "Drawing RB", 1000);
                        File.AppendAllText(logFilePath, "Drawing RB" + Environment.NewLine);

                        // Lower z position by 9800 steps
                        lowerZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                        // Draw 2376 steps (4.4mL)
                        drawLiquid(2376);

                        // Raise by 9800 steps
                        raiseZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                        // Move back to A3
                        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.RB_Bottle]);
                        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.RB_Bottle]);
                    }

                    AutoClosingMessageBox.Show("Dispensing RB in " + positions[i], "Dispensing", 1000);

                    // Dispense 420ul RB
                    dispenseLiquid(227);

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change RB Dispense box to finished color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Read Buffer Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "Read Buffer Dispensed" + Environment.NewLine);

                // --------------------------
                // ** RB Dispense Complete **

                // ** RB Wash **
                // -------------

                // Change Cartridges to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move from A4 to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.A4]);

                // Dispense remaining RB in bottle
                dispenseLiquid(200);

                // Move from RB_Bottle to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.RB_Bottle]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.RB_Bottle]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2268 steps (4.2mL)
                drawLiquid(2268);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.2mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ----------------------
                // ** RB Wash Complete **

                // ** Read Wells **
                // ----------------

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                // Board Initialization
                AutoClosingMessageBox.Show("Initializing Reader Board", "Initializing", 1000);
                File.AppendAllText(logFilePath, "Initializing Reader Board");

                StringBuilder sb = new StringBuilder(5000);
                bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

                MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());
                File.AppendAllText(logFilePath, "Test initializeBoard: " + initializeBoardBool + Environment.NewLine);

                // Define headers for all columns in output file
                string outputFileDataHeaders = "WellName" + delimiter + "Sample Name" + delimiter + "RFU" + delimiter + "TestResult" + delimiter + Environment.NewLine;

                // Add headers to output file
                File.AppendAllText(outputFilePath, outputFileDataHeaders);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_cart34.Visibility = Visibility.Hidden;
                inProgress_cart34_border.Visibility = Visibility.Hidden;

                File.AppendAllText(logFilePath, "In progress boxes hidden" + Environment.NewLine);

                File.AppendAllText(logFilePath, "Results display visible" + Environment.NewLine);

                resultsDisplay_cart34_border.Visibility = Visibility.Visible;
                resultsDisplay_cart34.Visibility = Visibility.Visible;

                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // Move from Wash_Bottle to E2 + Dispense_to_Read
                moveX((xPos[(int)steppingPositions.E2] + xPos[(int)steppingPositions.Dispense_to_Read]) - xPos[(int)steppingPositions.Wash_Bottle]);
                moveY((yPos[(int)steppingPositions.E2] + yPos[(int)steppingPositions.Dispense_to_Read]) - yPos[(int)steppingPositions.Wash_Bottle]);

                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                resultsTextboxes[4].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading E2", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading E2" + Environment.NewLine);

                StringBuilder sbE2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrE2 = testGetBoardValue(sbE2, sbE2.Capacity, excitationLedVoltage);
                double[] testArrayE2 = new double[5];
                Marshal.Copy(testBoardValuePtrE2, testArrayE2, 0, 5);
                string inputStringE2 = "";
                inputStringE2 += "Return Value: m_dAvgValue = " + testArrayE2[0] + "\n";
                inputStringE2 += "Return Value: m_dCumSum = " + testArrayE2[1] + "\n";
                inputStringE2 += "Return Value: m_dLEDtmp = " + testArrayE2[2] + "\n";
                inputStringE2 += "Return Value: m_dPDtmp = " + testArrayE2[3] + "\n";
                inputStringE2 += "Return Value: testGetBoardValue = " + testArrayE2[4] + "\n";

                File.AppendAllText(logFilePath, "E2 GetBoardValue: " + inputStringE2 + Environment.NewLine);

                if (testArrayE2[4] == 0)
                {
                    MessageBox.Show("Error reading E2. Check log for specifics");
                }

                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                resultsTextboxes[4].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayE2[0] - shiftFactors[(int)shiftsAndScales.E2]) * scaleFactors[(int)shiftsAndScales.E2];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.E2];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.E2],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                // Add results grid data for E2 to output file
                outputFileData = positions[(int)steppingPositions.E2] + delimiter + resultsTextboxes[4].Text + delimiter + raw_avg.ToString()
                                    + delimiter + testResult + delimiter + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[4].Text = raw_avg.ToString();
                resultsTextboxes[4].Foreground = Brushes.Black;

                // Loop through rest of reading steps
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Reading " + positions[i], "Reading", 2000);
                    File.AppendAllText(logFilePath, "Reading " + positions[i] + Environment.NewLine);

                    // Read sample in current well
                    StringBuilder sbR = new StringBuilder(100000);

                    IntPtr boardValuePtr = testGetBoardValue(sbR, sbR.Capacity, excitationLedVoltage);
                    double[] readingValues = new double[5];
                    Marshal.Copy(boardValuePtr, readingValues, 0, 5);
                    string readingInputString = "";
                    readingInputString += "Return Value: m_dAvgValue = " + readingValues[0] + "\n";
                    readingInputString += "Return Value: m_dCumSum = " + readingValues[1] + "\n";
                    readingInputString += "Return Value: m_dLEDtmp = " + readingValues[2] + "\n";
                    readingInputString += "Return Value: m_dPDtmp = " + readingValues[3] + "\n";
                    readingInputString += "Return Value: testGetBoardValue = " + readingValues[4] + "\n";

                    File.AppendAllText(logFilePath, positions[i] + " GetBoardValue: " + readingInputString + Environment.NewLine);

                    if (readingValues[4] == 0)
                    {
                        MessageBox.Show("Error reading " + positions[i] + ". Check log for specifics.");
                    }

                    // Update Results Grid for current well
                    raw_avg = (readingValues[0] - shiftFactors[i - 10]) * scaleFactors[i - 10];
                    raw_avg = Math.Round(raw_avg, 3);

                    TC_rdg = raw_avg;

                    diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                    testResult = "NEG";

                    if (diff > threshold)
                    {
                        testResult = "POS";
                    }

                    sampleName = positions[i];

                    testResults.Add(new TestResults()
                    {
                        WellName = positions[i],
                        BackgroundReading = bgd_rdg.ToString(),
                        Threshold = threshold.ToString(),
                        RawAvg = raw_avg.ToString(),
                        TC_rdg = TC_rdg.ToString(),
                        TestResult = testResult,
                        SampleName = sampleName
                    });

                    // Add results grid data for current well to output file
                    outputFileData = positions[i] + delimiter + resultsTextboxes[i - 10].Text + delimiter + raw_avg.ToString() + delimiter + testResult + delimiter + Environment.NewLine;

                    File.AppendAllText(outputFilePath, outputFileData);

                    resultsTextboxes[i - 10].Text = raw_avg.ToString();
                    resultsTextboxes[i - 10].Foreground = Brushes.Black;

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        File.AppendAllText(dataFilePath, sbR.ToString() + Environment.NewLine);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                        resultsTextboxes[i - 9].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                AutoClosingMessageBox.Show("All wells read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All wells read" + Environment.NewLine);

                testCloseTasksAndChannels();

                // ----------------------------
                // ** Reading Wells Complete **

                // ** Move Back to Load **
                // -----------------------

                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.A4] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.A4] + yPos[(int)steppingPositions.Dispense_to_Read]));

                AutoClosingMessageBox.Show("Reading complete", "Results", 2000);
                File.AppendAllText(logFilePath, "Reading complete" + Environment.NewLine);

                isReading = false;
                File.AppendAllText(logFilePath, "Reading completed" + Environment.NewLine);

                // --------------
                // ** Complete **
            }

            else if (sampleNum_tb.Text == "2")
            {                
                // 540 steps = 1mL

                // ** Start of moving steps **
                // ---------------------------

                // ** Sample Dispensing, Incubation, and Drain **
                // --------------------------------------------

                //// ** Sample Dispensing **
                //// ---------------------

                //// Move to Sample Bottle
                //moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Load]);
                //moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Load]);

                //// Draw Sample for row 2
                //AutoClosingMessageBox.Show("Drawing Sample", "Drawing Sample", 1000);
                //File.AppendAllText(logFilePath, "Drawing Sample" + Environment.NewLine);

                //// Lower Z by 9500 steps
                //lowerZPosition(9500);

                //// Draw 820 steps (1.5mL + extra)
                //drawLiquid(820);

                //// Raise Z by 9500 steps
                //raiseZPosition(9500);

                //// Change Sample Dispense Box to in progress color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Foreground = Brushes.Black;

                ////**E2**//
                //// Move from Sample bottle to E2
                //moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.HBSS_Bottle]);
                //moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.HBSS_Bottle]);

                //// Change E2 to in progress color
                //inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //AutoClosingMessageBox.Show("Dispensing Sample in E2", "Dispensing", 1000);

                //// Dispense 300ul Sample in E2
                //dispenseLiquid(164);

                //// Change E2 to finished color
                //inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //// Dispense Sample in remaining wells
                //for (int i = 15; i < 19; i++)
                //{
                //    // Move to next well
                //    moveY(yPos[i] - yPos[i - 1]);
                //    moveX(xPos[i] - xPos[i - 1]);

                //    // Change current well to in progress color
                //    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //    AutoClosingMessageBox.Show("Dispensing Sample in " + positions[i], "Dispensing", 1000);

                //    // dispense remaining liquid in last well
                //    if (i == 18)
                //    {
                //        // Dispense 300ul Sample + remaining
                //        dispenseLiquid(164);

                //        // wait 3 seconds and dispense remaining amount
                //        Task.Delay(3000).Wait();

                //        dispenseLiquid(50);
                //    }
                //    else
                //    {
                //        // Dispense 300ul Sample
                //        dispenseLiquid(164);
                //    }

                //    // Change current well to finished color and next well to in progress color except for last time
                //    if (i == 18)
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //    }
                //    else
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //    }
                //}

                //// Change Sample Dispense Box to Finished Color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                ////MessageBox.Show("Sample Dispensed");
                //AutoClosingMessageBox.Show("Sample Dispensed", "Dispensing Complete", 1000);
                //File.AppendAllText(logFilePath, "Sample Dispensed" + Environment.NewLine);

                //// -----------------------------
                //// ** Sample Dispense Complete **

                //// ** Sample Wash **
                //// ---------------

                //// Change wells to gray
                //for (int i = 4; i < 9; i++)
                //{
                //    inProgressEllipses[i].Fill = Brushes.Gray;
                //}

                //AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                //// Move from A2 to Wash_Bottle
                //moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A2]);
                //moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A2]);

                //// Lower pipette tips
                //lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Draw 2430 steps (4.5mL)
                //drawLiquid(2430);

                //// Raise pipette tips
                //raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Dispense 2500 steps (4.5mL + extra)
                //dispenseLiquid(2500);

                //Task.Delay(5000).Wait();

                //dispenseLiquid(300);

                //// ------------------------
                //// ** Sample Wash Complete **

                // ** Sample Incubation and Draining **
                // ----------------------------------

                // Change Sample Incubation and Draining Box to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;

                // Change wells to in progress color
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes1 = Int32.Parse(incubationTime1_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes1 + " minutes", "Incubating", 1000);

                for (int i = 0; i < incubationMinutes1; i++)
                {
                    int remaining = incubationMinutes1 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Load]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Load]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                drainMinutes = double.Parse(drainTime_tb.Text);
                drainTime = Convert.ToInt32(drainMinutes * 60 * 1000);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for samples to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for samples to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Sample Draining Box to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change wells to finished color
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                //MessageBox.Show("Sample Draining Complete");
                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // ----------------------------------------
                // ** Sample Incubation and Drain Complete **

                // -----------------------------------------------------
                // ** Sample Dispensing, Incubation, and Drain Complete **

                // ** Probe Dispensing **
                // ----------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Change Probe Dispense Box to in progress color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Foreground = Brushes.Black;

                // Change cartridge wells to gray
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move to Probe Bottle
                moveX(xPos[(int)steppingPositions.Probe_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.Probe_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Draw Probe for row 2
                AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                // Lower Z by 9500 steps
                lowerZPosition(9500);

                // Draw 820 steps (1.5mL + extra)
                drawLiquid(820);

                // Raise Z by 9500 steps
                raiseZPosition(9500);

                //**E2**//
                // Move from Probe bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.Probe_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.Probe_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in E2", "Dispensing", 1000);

                // Dispense 300ul Probe in E2
                dispenseLiquid(164);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense Probe in remaining wells
                for (int i = 15; i < 19; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Dispensing Probe in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 18)
                    {
                        // Dispense 300ul Probe + remaining
                        dispenseLiquid(174);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(50);
                    }
                    else
                    {
                        // Dispense 300ul Probe
                        dispenseLiquid(164);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 18)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change probe dispense box to finished color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Probe Dispensed", "Dispensing Complete", 1000);
                File.AppendAllText(logFilePath, "Probe Dispensed" + Environment.NewLine);

                // -----------------------------
                // ** Probe Dispense Complete **

                // ** Probe Wash **
                // ----------------

                // Change wells to gray
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Probe Tip", "Cleaning", 1000);

                // Move from A2 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A2]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** Probe Wash Complete **

                // ** Probe Incubation and Drain **
                // --------------------------------

                // Change Probe Draining Box and Cartridge to in progress color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Foreground = Brushes.Black;
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes2 = Int32.Parse(incubationTime2_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes2 + " minutes", "Incubating", 3000);

                for (int i = 0; i < incubationMinutes2; i++)
                {
                    int remaining = incubationMinutes2 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for probe to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for probe to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult2 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult2 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult2 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Probe Draining Box and Cartridge to finished color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -----------------------------------------
                // ** Probe Incubation and Drain Complete **

                // ** HBSS Dispense **
                // -------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to HBSS_Bottle
                moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Draw 1188 steps (2.2mL)
                drawLiquid(1188);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Change HBSS Dispense Box to in progress color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing HBSS", "Dispensing", 1000);

                //**E2**//
                // Move from HBSS bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.HBSS_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.HBSS_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing HBSS in E2", "Dispensing", 1000);

                // Dispense 400ul HBSS in E2
                dispenseLiquid(216);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense HBSS in remaining wells
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Before dispensing in A3, go back to HBSS bottle and draw more
                    if (i == 19)
                    {
                        // Move from A3 to HBSS bottle
                        moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.A3]);
                        moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.A3]);

                        // Dispense remaining liquid before drawing more
                        dispenseLiquid(200);

                        // Draw HBSS for rows 3 and 4
                        AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                        File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                        // Lower z position by 9800 steps
                        lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Draw 2200 steps (4mL + extra)
                        drawLiquid(2200);

                        // Raise by 9800 steps
                        raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Move back to A3
                        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.HBSS_Bottle]);
                        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.HBSS_Bottle]);
                    }

                    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 28)
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(100);
                    }
                    else
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change HBSS Dispense box to finished color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                // ----------------------------
                // ** HBSS Dispense Complete **

                // ** HBSS Wash **
                // ----------------

                // Change wells to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                // Move from A4 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** HBSS Wash Complete **

                // ** HBSS Drain **
                // ----------------

                // Change HBSS Draining Box and Cartridge to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for HBSS to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for HBSS to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult3 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult3 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult3 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change HBSS Draining Box and Cartridge to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -------------------------
                // ** HBSS Drain Complete **

                // ** RB Dispense **
                // -----------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Draw 1145 steps (2.1mL + extra)
                drawLiquid(1145);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Change RB Dispense Box to in progress color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing RB", "Dispensing", 1000);

                //**E2**//
                // Move from RB bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.RB_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.RB_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in E2", "Dispensing", 1000);

                // Dispense 420ul RB in E2
                dispenseLiquid(227);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense RB in remaining wells
                for (int i = 15; i < 19; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Dispensing RB in " + positions[i], "Dispensing", 1000);

                    dispenseLiquid(227);

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 18)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change RB Dispense box to finished color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Read Buffer Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "Read Buffer Dispensed" + Environment.NewLine);

                // --------------------------
                // ** RB Dispense Complete **

                // ** RB Wash **
                // -------------

                // Change Cartridges to gray
                for (int i = 4; i < 9; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move from A2 to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.A2]);

                // Dispense remaining RB in bottle
                dispenseLiquid(200);

                // Move from RB_Bottle to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.RB_Bottle]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.RB_Bottle]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2268 steps (4.2mL)
                drawLiquid(2268);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.2mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ----------------------
                // ** RB Wash Complete **

                // ** Read Wells **
                // ----------------

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                // Board Initialization
                AutoClosingMessageBox.Show("Initializing Reader Board", "Initializing", 1000);
                File.AppendAllText(logFilePath, "Initializing Reader Board");

                StringBuilder sb = new StringBuilder(5000);
                bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

                MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());
                File.AppendAllText(logFilePath, "Test initializeBoard: " + initializeBoardBool + Environment.NewLine);

                // Define headers for all columns in output file
                string outputFileDataHeaders = "WellName" + delimiter + "Sample Name" + delimiter + "RFU" + delimiter + "TestResult" + delimiter + Environment.NewLine;

                // Add headers to output file
                File.AppendAllText(outputFilePath, outputFileDataHeaders);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_cart34.Visibility = Visibility.Hidden;
                inProgress_cart34_border.Visibility = Visibility.Hidden;

                File.AppendAllText(logFilePath, "In progress boxes hidden" + Environment.NewLine);

                File.AppendAllText(logFilePath, "Results display visible" + Environment.NewLine);

                resultsDisplay_cart34_border.Visibility = Visibility.Visible;
                resultsDisplay_cart34.Visibility = Visibility.Visible;

                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // Move from Wash_Bottle to E2 + Dispense_to_Read
                moveX((xPos[(int)steppingPositions.E2] + xPos[(int)steppingPositions.Dispense_to_Read]) - xPos[(int)steppingPositions.Wash_Bottle]);
                moveY((yPos[(int)steppingPositions.E2] + yPos[(int)steppingPositions.Dispense_to_Read]) - yPos[(int)steppingPositions.Wash_Bottle]);

                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                resultsTextboxes[4].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading E2", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading E2" + Environment.NewLine);

                StringBuilder sbE2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrE2 = testGetBoardValue(sbE2, sbE2.Capacity, excitationLedVoltage);
                double[] testArrayE2 = new double[5];
                Marshal.Copy(testBoardValuePtrE2, testArrayE2, 0, 5);
                string inputStringE2 = "";
                inputStringE2 += "Return Value: m_dAvgValue = " + testArrayE2[0] + "\n";
                inputStringE2 += "Return Value: m_dCumSum = " + testArrayE2[1] + "\n";
                inputStringE2 += "Return Value: m_dLEDtmp = " + testArrayE2[2] + "\n";
                inputStringE2 += "Return Value: m_dPDtmp = " + testArrayE2[3] + "\n";
                inputStringE2 += "Return Value: testGetBoardValue = " + testArrayE2[4] + "\n";

                File.AppendAllText(logFilePath, "E2 GetBoardValue: " + inputStringE2 + Environment.NewLine);

                if (testArrayE2[4] == 0)
                {
                    MessageBox.Show("Error reading E2. Check log for specifics");
                }

                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                resultsTextboxes[4].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayE2[0] - shiftFactors[(int)shiftsAndScales.E2]) * scaleFactors[(int)shiftsAndScales.E2];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.E2];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.E2],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                // Add results grid data for E2 to output file
                outputFileData = positions[(int)steppingPositions.E2] + delimiter + resultsTextboxes[4].Text +
                                 delimiter + raw_avg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[4].Text = raw_avg.ToString();
                resultsTextboxes[4].Foreground = Brushes.Black;

                // Loop through rest of reading steps
                for (int i = 15; i < 19; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Reading " + positions[i], "Reading", 2000);
                    File.AppendAllText(logFilePath, "Reading " + positions[i] + Environment.NewLine);

                    // Read sample in current well
                    StringBuilder sbR = new StringBuilder(100000);

                    IntPtr boardValuePtr = testGetBoardValue(sbR, sbR.Capacity, excitationLedVoltage);
                    double[] readingValues = new double[5];
                    Marshal.Copy(boardValuePtr, readingValues, 0, 5);
                    string readingInputString = "";
                    readingInputString += "Return Value: m_dAvgValue = " + readingValues[0] + "\n";
                    readingInputString += "Return Value: m_dCumSum = " + readingValues[1] + "\n";
                    readingInputString += "Return Value: m_dLEDtmp = " + readingValues[2] + "\n";
                    readingInputString += "Return Value: m_dPDtmp = " + readingValues[3] + "\n";
                    readingInputString += "Return Value: testGetBoardValue = " + readingValues[4] + "\n";

                    File.AppendAllText(logFilePath, positions[i] + " GetBoardValue: " + readingInputString + Environment.NewLine);

                    if (readingValues[4] == 0)
                    {
                        MessageBox.Show("Error reading " + positions[i] + ". Check log for specifics.");
                    }

                    // Update Results Grid for current well
                    raw_avg = (readingValues[0] - shiftFactors[i - 10]) * scaleFactors[i - 10];
                    raw_avg = Math.Round(raw_avg, 3);

                    TC_rdg = raw_avg;

                    diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                    testResult = "NEG";

                    if (diff > threshold)
                    {
                        testResult = "POS";
                    }

                    sampleName = positions[i];

                    testResults.Add(new TestResults()
                    {
                        WellName = positions[i],
                        BackgroundReading = bgd_rdg.ToString(),
                        Threshold = threshold.ToString(),
                        RawAvg = raw_avg.ToString(),
                        TC_rdg = TC_rdg.ToString(),
                        TestResult = testResult,
                        SampleName = sampleName
                    });

                    // Add results grid data for current well to output file
                    outputFileData = positions[i] + delimiter + resultsTextboxes[i - 10].Text +
                                     delimiter + raw_avg.ToString() + delimiter + testResult + Environment.NewLine;

                    File.AppendAllText(outputFilePath, outputFileData);

                    resultsTextboxes[i - 10].Text = raw_avg.ToString();
                    resultsTextboxes[i - 10].Foreground = Brushes.Black;

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 18)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        File.AppendAllText(dataFilePath, sbR.ToString() + Environment.NewLine);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                        resultsTextboxes[i - 9].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                AutoClosingMessageBox.Show("All wells read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All wells read" + Environment.NewLine);

                testCloseTasksAndChannels();

                // ----------------------------
                // ** Reading Wells Complete **

                // ** Move Back to Load **
                // -----------------------

                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.A2] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.A2] + yPos[(int)steppingPositions.Dispense_to_Read]));

                AutoClosingMessageBox.Show("Reading complete", "Results", 2000);
                File.AppendAllText(logFilePath, "Reading complete" + Environment.NewLine);

                isReading = false;
                File.AppendAllText(logFilePath, "Reading completed" + Environment.NewLine);

                // --------------
                // ** Complete **
            }

            else if (sampleNum_tb.Text == "3")
            {                
                // 540 steps = 1mL

                // ** Start of moving steps **
                // ---------------------------

                // ** HBSS Dispensing, Incubation, and Drain **
                // --------------------------------------------

                // ** HBSS Dispensing **
                // ---------------------

                //// Move to HBSS Bottle
                //moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Load]);
                //moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Load]);

                //// Draw HBSS for row 3
                //AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                //File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                //// Lower Z by 9500 steps
                //lowerZPosition(9500);

                //// Draw 820 steps (1.5mL + extra)
                //drawLiquid(820);

                //// Raise Z by 9500 steps
                //raiseZPosition(9500);

                //// Change HBSS Dispense Box to in progress color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Foreground = Brushes.Black;

                ////**A3**//
                //// Move from HBSS bottle to A3
                //moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.HBSS_Bottle]);
                //moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.HBSS_Bottle]);

                //// Change A3 to in progress color
                //inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //AutoClosingMessageBox.Show("Dispensing HBSS in A3", "Dispensing", 1000);

                //// Dispense 300ul HBSS in A3
                //dispenseLiquid(164);

                //// Change A3 to finished color
                //inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //// Dispense HBSS in remaining wells
                //for (int i = 20; i < 24; i++)
                //{
                //    // Move to next well
                //    moveY(yPos[i] - yPos[i - 1]);
                //    moveX(xPos[i] - xPos[i - 1]);

                //    // Change current well to in progress color
                //    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                //    // dispense remaining liquid in last well
                //    if (i == 23)
                //    {
                //        // Dispense 300ul HBSS + remaining
                //        dispenseLiquid(164);

                //        // wait 3 seconds and dispense remaining amount
                //        Task.Delay(3000).Wait();

                //        dispenseLiquid(50);
                //    }
                //    else
                //    {
                //        // Dispense 300ul HBSS
                //        dispenseLiquid(164);
                //    }

                //    // Change current well to finished color and next well to in progress color except for last time
                //    if (i == 23)
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //    }
                //    else
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //    }
                //}

                //// Change HBSS Dispense Box to Finished Color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                ////MessageBox.Show("HBSS Dispensed");
                //AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 1000);
                //File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                //// -----------------------------
                //// ** HBSS Dispense Complete **

                //// ** HBSS Wash **
                //// ---------------

                //// Change wells to gray
                //for (int i = 9; i < 14; i++)
                //{
                //    inProgressEllipses[i].Fill = Brushes.Gray;
                //}

                //AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                //// Move from E3 to Wash_Bottle
                //moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.E3]);
                //moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.E3]);

                //// Lower pipette tips
                //lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Draw 2430 steps (4.5mL)
                //drawLiquid(2430);

                //// Raise pipette tips
                //raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Dispense 2500 steps (4.5mL + extra)
                //dispenseLiquid(2500);

                //Task.Delay(5000).Wait();

                //dispenseLiquid(300);

                //// ------------------------
                //// ** HBSS Wash Complete **

                // ** HBSS Incubation and Draining **
                // ----------------------------------

                // Change HBSS Incubation and Draining Box to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;

                // Change wells to in progress color
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes1 = Int32.Parse(incubationTime1_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes1 + " minutes", "Incubating", 1000);

                for (int i = 0; i < incubationMinutes1; i++)
                {
                    int remaining = incubationMinutes1 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Load]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Load]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                drainMinutes = double.Parse(drainTime_tb.Text);
                drainTime = Convert.ToInt32(drainMinutes * 60 * 1000);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for samples to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for samples to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Sample Draining Box to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change wells to finished color
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }
                                
                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // ----------------------------------------
                // ** Sample Incubation and Drain Complete **

                // -----------------------------------------------------
                // ** Sample Dispensing, Incubation, and Drain Complete **

                // ** Probe Dispensing **
                // ----------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Change Probe Dispense Box to in progress color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Foreground = Brushes.Black;

                // Change cartridge wells to gray
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move to Probe Bottle
                moveX(xPos[(int)steppingPositions.Probe_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.Probe_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Draw Probe for row 3
                AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                // Lower Z by 9500 steps
                lowerZPosition(9500);

                // Draw 820 steps (1.5mL + extra)
                drawLiquid(820);

                // Raise Z by 9500 steps
                raiseZPosition(9500);

                //**A3**//
                // Move from Probe bottle to A3
                moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.Probe_Bottle]);
                moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.Probe_Bottle]);

                // Change A3 to in progress color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A3", "Dispensing", 1000);

                // Dispense 300ul Probe in A3
                dispenseLiquid(164);

                // Change A3 to finished color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense Probe in remaining wells
                for (int i = 20; i < 24; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Dispensing Probe in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 23)
                    {
                        // Dispense 300ul Probe + remaining
                        dispenseLiquid(174);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(50);
                    }
                    else
                    {
                        // Dispense 300ul Probe
                        dispenseLiquid(164);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 23)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change probe dispense box to finished color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Probe Dispensed", "Dispensing Complete", 1000);
                File.AppendAllText(logFilePath, "Probe Dispensed" + Environment.NewLine);

                // -----------------------------
                // ** Probe Dispense Complete **

                // ** Probe Wash **
                // ----------------

                // Change wells to gray
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Probe Tip", "Cleaning", 1000);

                // Move from E3 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.E3]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.E3]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** Probe Wash Complete **

                // ** Probe Incubation and Drain **
                // --------------------------------

                // Change Probe Draining Box and Cartridge to in progress color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Foreground = Brushes.Black;
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes2 = Int32.Parse(incubationTime2_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes2 + " minutes", "Incubating", 3000);

                for (int i = 0; i < incubationMinutes2; i++)
                {
                    int remaining = incubationMinutes2 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for probe to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for probe to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult2 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult2 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult2 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Probe Draining Box and Cartridge to finished color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -----------------------------------------
                // ** Probe Incubation and Drain Complete **

                // ** HBSS Dispense **
                // -------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to HBSS_Bottle
                moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Draw 1188 steps (2.2mL)
                drawLiquid(1188);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Change HBSS Dispense Box to in progress color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing HBSS", "Dispensing", 1000);

                //**E2**//
                // Move from HBSS bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.HBSS_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.HBSS_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing HBSS in E2", "Dispensing", 1000);

                // Dispense 400ul HBSS in E2
                dispenseLiquid(216);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense HBSS in remaining wells
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Before dispensing in A3, go back to HBSS bottle and draw more
                    if (i == 19)
                    {
                        // Move from A3 to HBSS bottle
                        moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.A3]);
                        moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.A3]);

                        // Dispense remaining liquid before drawing more
                        dispenseLiquid(200);

                        // Draw HBSS for rows 3 and 4
                        AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                        File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                        // Lower z position by 9800 steps
                        lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Draw 2200 steps (4mL + extra)
                        drawLiquid(2200);

                        // Raise by 9800 steps
                        raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Move back to A3
                        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.HBSS_Bottle]);
                        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.HBSS_Bottle]);
                    }

                    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 28)
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(100);
                    }
                    else
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change HBSS Dispense box to finished color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                // ----------------------------
                // ** HBSS Dispense Complete **

                // ** HBSS Wash **
                // ----------------

                // Change wells to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                // Move from A4 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** HBSS Wash Complete **

                // ** HBSS Drain **
                // ----------------

                // Change HBSS Draining Box and Cartridge to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for HBSS to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for HBSS to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult3 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult3 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult3 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change HBSS Draining Box and Cartridge to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -------------------------
                // ** HBSS Drain Complete **

                // ** RB Dispense **
                // -----------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Draw 1145 steps (2.1mL + extra)
                drawLiquid(1145);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Change RB Dispense Box to in progress color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing RB", "Dispensing", 1000);

                //**A3**//
                // Move from RB bottle to A3
                moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.RB_Bottle]);
                moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.RB_Bottle]);

                // Change A3 to in progress color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A3", "Dispensing", 1000);

                // Dispense 420ul RB in A3
                dispenseLiquid(227);

                // Change A3 to finished color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense RB in remaining wells
                for (int i = 20; i < 24; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Dispensing RB in " + positions[i], "Dispensing", 1000);

                    dispenseLiquid(227);

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 23)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change RB Dispense box to finished color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Read Buffer Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "Read Buffer Dispensed" + Environment.NewLine);

                // --------------------------
                // ** RB Dispense Complete **

                // ** RB Wash **
                // -------------

                // Change Cartridges to gray
                for (int i = 9; i < 14; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move from E3 to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.E3]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.E3]);

                // Dispense remaining RB in bottle
                dispenseLiquid(200);

                // Move from RB_Bottle to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.RB_Bottle]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.RB_Bottle]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2268 steps (4.2mL)
                drawLiquid(2268);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.2mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ----------------------
                // ** RB Wash Complete **

                // ** Read Wells **
                // ----------------

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                // Board Initialization
                AutoClosingMessageBox.Show("Initializing Reader Board", "Initializing", 1000);
                File.AppendAllText(logFilePath, "Initializing Reader Board");

                StringBuilder sb = new StringBuilder(5000);
                bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

                MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());
                File.AppendAllText(logFilePath, "Test initializeBoard: " + initializeBoardBool + Environment.NewLine);

                // Define headers for all columns in output file
                string outputFileDataHeaders = "WellName" + delimiter + "Sample Name" + delimiter + "RFU" + delimiter + "TestResult" + delimiter + Environment.NewLine;

                // Add headers to output file
                File.AppendAllText(outputFilePath, outputFileDataHeaders);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_cart34.Visibility = Visibility.Hidden;
                inProgress_cart34_border.Visibility = Visibility.Hidden;

                File.AppendAllText(logFilePath, "In progress boxes hidden" + Environment.NewLine);

                File.AppendAllText(logFilePath, "Results display visible" + Environment.NewLine);

                resultsDisplay_cart34_border.Visibility = Visibility.Visible;
                resultsDisplay_cart34.Visibility = Visibility.Visible;

                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // Move from Wash_Bottle to A3 + Dispense_to_Read
                moveX((xPos[(int)steppingPositions.A3] + xPos[(int)steppingPositions.Dispense_to_Read]) - xPos[(int)steppingPositions.Wash_Bottle]);
                moveY((yPos[(int)steppingPositions.A3] + yPos[(int)steppingPositions.Dispense_to_Read]) - yPos[(int)steppingPositions.Wash_Bottle]);

                inProgressEllipses[9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                resultsTextboxes[9].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A3", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading A3" + Environment.NewLine);

                StringBuilder sbA3 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA3 = testGetBoardValue(sbA3, sbA3.Capacity, excitationLedVoltage);
                double[] testArrayA3 = new double[5];
                Marshal.Copy(testBoardValuePtrA3, testArrayA3, 0, 5);
                string inputStringA3 = "";
                inputStringA3 += "Return Value: m_dAvgValue = " + testArrayA3[0] + "\n";
                inputStringA3 += "Return Value: m_dCumSum = " + testArrayA3[1] + "\n";
                inputStringA3 += "Return Value: m_dLEDtmp = " + testArrayA3[2] + "\n";
                inputStringA3 += "Return Value: m_dPDtmp = " + testArrayA3[3] + "\n";
                inputStringA3 += "Return Value: testGetBoardValue = " + testArrayA3[4] + "\n";

                File.AppendAllText(logFilePath, "A3 GetBoardValue: " + inputStringA3 + Environment.NewLine);

                if (testArrayA3[4] == 0)
                {
                    MessageBox.Show("Error reading A3. Check log for specifics");
                }

                inProgressEllipses[9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                resultsTextboxes[9].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA3[0] - shiftFactors[(int)shiftsAndScales.A3]) * scaleFactors[(int)shiftsAndScales.A3];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A3];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A3],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                // Add results grid data for A3 to output file
                outputFileData = positions[(int)steppingPositions.A3] + delimiter + resultsTextboxes[9].Text +
                                            delimiter + raw_avg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[9].Text = raw_avg.ToString();
                resultsTextboxes[9].Foreground = Brushes.Black;

                // Loop through rest of reading steps
                for (int i = 20; i < 24; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Reading " + positions[i], "Reading", 2000);
                    File.AppendAllText(logFilePath, "Reading " + positions[i] + Environment.NewLine);

                    // Read sample in current well
                    StringBuilder sbR = new StringBuilder(100000);

                    IntPtr boardValuePtr = testGetBoardValue(sbR, sbR.Capacity, excitationLedVoltage);
                    double[] readingValues = new double[5];
                    Marshal.Copy(boardValuePtr, readingValues, 0, 5);
                    string readingInputString = "";
                    readingInputString += "Return Value: m_dAvgValue = " + readingValues[0] + "\n";
                    readingInputString += "Return Value: m_dCumSum = " + readingValues[1] + "\n";
                    readingInputString += "Return Value: m_dLEDtmp = " + readingValues[2] + "\n";
                    readingInputString += "Return Value: m_dPDtmp = " + readingValues[3] + "\n";
                    readingInputString += "Return Value: testGetBoardValue = " + readingValues[4] + "\n";

                    File.AppendAllText(logFilePath, positions[i] + " GetBoardValue: " + readingInputString + Environment.NewLine);

                    if (readingValues[4] == 0)
                    {
                        MessageBox.Show("Error reading " + positions[i] + ". Check log for specifics.");
                    }

                    // Update Results Grid for current well
                    raw_avg = (readingValues[0] - shiftFactors[i - 10]) * scaleFactors[i - 10];
                    raw_avg = Math.Round(raw_avg, 3);

                    TC_rdg = raw_avg;

                    diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                    testResult = "NEG";

                    if (diff > threshold)
                    {
                        testResult = "POS";
                    }

                    sampleName = positions[i];

                    testResults.Add(new TestResults()
                    {
                        WellName = positions[i],
                        BackgroundReading = bgd_rdg.ToString(),
                        Threshold = threshold.ToString(),
                        RawAvg = raw_avg.ToString(),
                        TC_rdg = TC_rdg.ToString(),
                        TestResult = testResult,
                        SampleName = sampleName
                    });

                    // Add results grid data for current well to output file
                    outputFileData = positions[i] + delimiter + resultsTextboxes[i - 10].Text +
                                            delimiter + raw_avg.ToString() + delimiter + testResult + Environment.NewLine;

                    File.AppendAllText(outputFilePath, outputFileData);

                    resultsTextboxes[i - 10].Text = raw_avg.ToString();
                    resultsTextboxes[i - 10].Foreground = Brushes.Black;

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 23)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        File.AppendAllText(dataFilePath, sbR.ToString() + Environment.NewLine);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                        resultsTextboxes[i - 9].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                AutoClosingMessageBox.Show("All wells read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All wells read" + Environment.NewLine);

                testCloseTasksAndChannels();

                // ----------------------------
                // ** Reading Wells Complete **

                // ** Move Back to Load **
                // -----------------------

                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.E3] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.E3] + yPos[(int)steppingPositions.Dispense_to_Read]));

                AutoClosingMessageBox.Show("Reading complete", "Results", 2000);
                File.AppendAllText(logFilePath, "Reading complete" + Environment.NewLine);

                isReading = false;
                File.AppendAllText(logFilePath, "Reading completed" + Environment.NewLine);

                // --------------
                // ** Complete **
            }

            else if (sampleNum_tb.Text == "4")
            {                
                // 540 steps = 1mL

                // ** Start of moving steps **
                // ---------------------------

                // ** HBSS Dispensing, Incubation, and Drain **
                // --------------------------------------------

                // ** HBSS Dispensing **
                // ---------------------

                //// Move to HBSS Bottle
                //moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Load]);
                //moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Load]);

                //// Draw HBSS for row 4
                //AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                //File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                //// Lower Z by 9500 steps
                //lowerZPosition(9500);

                //// Draw 820 steps (1.5mL + extra)
                //drawLiquid(820);

                //// Raise Z by 9500 steps
                //raiseZPosition(9500);

                //// Change HBSS Dispense Box to in progress color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //hbssDispense_tb.Foreground = Brushes.Black;

                ////**E4**//
                //// Move from HBSS bottle to E4
                //moveY(yPos[(int)steppingPositions.E4] - yPos[(int)steppingPositions.HBSS_Bottle]);
                //moveX(xPos[(int)steppingPositions.E4] - xPos[(int)steppingPositions.HBSS_Bottle]);

                //// Change E4 to in progress color
                //inProgressE4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //AutoClosingMessageBox.Show("Dispensing HBSS in E4", "Dispensing", 1000);

                //// Dispense 300ul HBSS in E4
                //dispenseLiquid(164);

                //// Change E4 to finished color
                //inProgressE4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //// Dispense HBSS in remaining wells
                //for (int i = 25; i < 29; i++)
                //{
                //    // Move to next well
                //    moveY(yPos[i] - yPos[i - 1]);
                //    moveX(xPos[i] - xPos[i - 1]);

                //    // Change current well to in progress color
                //    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                //    // dispense remaining liquid in last well
                //    if (i == 28)
                //    {
                //        // Dispense 300ul HBSS + remaining
                //        dispenseLiquid(164);

                //        // wait 3 seconds and dispense remaining amount
                //        Task.Delay(3000).Wait();

                //        dispenseLiquid(50);
                //    }
                //    else
                //    {
                //        // Dispense 300ul HBSS
                //        dispenseLiquid(164);
                //    }

                //    // Change current well to finished color and next well to in progress color except for last time
                //    if (i == 28)
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //    }
                //    else
                //    {
                //        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                //    }
                //}

                //// Change HBSS Dispense Box to Finished Color
                //hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                //hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                ////MessageBox.Show("HBSS Dispensed");
                //AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 1000);
                //File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                //// -----------------------------
                //// ** HBSS Dispense Complete **

                //// ** HBSS Wash **
                //// ---------------

                //// Change wells to gray
                //for (int i = 14; i < 19; i++)
                //{
                //    inProgressEllipses[i].Fill = Brushes.Gray;
                //}

                //AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                //// Move from A4 to Wash_Bottle
                //moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                //moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                //// Lower pipette tips
                //lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Draw 2430 steps (4.5mL)
                //drawLiquid(2430);

                //// Raise pipette tips
                //raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                //// Dispense 2500 steps (4.5mL + extra)
                //dispenseLiquid(2500);

                //Task.Delay(5000).Wait();

                //dispenseLiquid(300);

                //// ------------------------
                //// ** HBSS Wash Complete **

                // ** HBSS Incubation and Draining **
                // ----------------------------------

                // Change HBSS Incubation and Draining Box to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;

                // Change wells to in progress color
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes1 = Int32.Parse(incubationTime1_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes1 + " minutes", "Incubating", 1000);

                for (int i = 0; i < incubationMinutes1; i++)
                {
                    int remaining = incubationMinutes1 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Load]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Load]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                drainMinutes = double.Parse(drainTime_tb.Text);
                drainTime = Convert.ToInt32(drainMinutes * 60 * 1000);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for samples to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for samples to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change HBSS Draining Box to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change wells to finished color
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // ----------------------------------------
                // ** Sample Incubation and Drain Complete **

                // -----------------------------------------------------
                // ** Sample Dispensing, Incubation, and Drain Complete **

                // ** Probe Dispensing **
                // ----------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Change Probe Dispense Box to in progress color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDispense_tb.Foreground = Brushes.Black;

                // Change cartridge wells to gray
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move to Probe Bottle
                moveX(xPos[(int)steppingPositions.Probe_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.Probe_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Draw Probe for row 4
                AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                // Lower Z by 9500 steps
                lowerZPosition(9500);

                // Draw 820 steps (1.5mL + extra)
                drawLiquid(820);

                // Raise Z by 9500 steps
                raiseZPosition(9500);

                //**E4**//
                // Move from Probe bottle to E4
                moveY(yPos[(int)steppingPositions.E4] - yPos[(int)steppingPositions.Probe_Bottle]);
                moveX(xPos[(int)steppingPositions.E4] - xPos[(int)steppingPositions.Probe_Bottle]);

                // Change E4 to in progress color
                inProgressE4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in E4", "Dispensing", 1000);

                // Dispense 300ul Probe in E4
                dispenseLiquid(164);

                // Change E4 to finished color
                inProgressE4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense Probe in remaining wells
                for (int i = 25; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Dispensing Probe in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 28)
                    {
                        // Dispense 300ul Probe + remaining
                        dispenseLiquid(174);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(50);
                    }
                    else
                    {
                        // Dispense 300ul Probe
                        dispenseLiquid(164);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change probe dispense box to finished color
                probeDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Probe Dispensed", "Dispensing Complete", 1000);
                File.AppendAllText(logFilePath, "Probe Dispensed" + Environment.NewLine);

                // -----------------------------
                // ** Probe Dispense Complete **

                // ** Probe Wash **
                // ----------------

                // Change wells to gray
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Probe Tip", "Cleaning", 1000);

                // Move from A4 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** Probe Wash Complete **

                // ** Probe Incubation and Drain **
                // --------------------------------

                // Change Probe Draining Box and Cartridge to in progress color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                probeDrain_tb.Foreground = Brushes.Black;
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Incubate for the amount of time entered
                incubationMinutes2 = Int32.Parse(incubationTime2_tb.Text);

                AutoClosingMessageBox.Show("Incubating for " + incubationMinutes2 + " minutes", "Incubating", 3000);

                for (int i = 0; i < incubationMinutes2; i++)
                {
                    int remaining = incubationMinutes2 - i;
                    incubationRemaining_tb.Text = (remaining).ToString();

                    if (remaining == 1)
                    {
                        AutoClosingMessageBox.Show(remaining + " minute remaining", "Incubating", 1000);
                        MediaPlayer sound1 = new MediaPlayer();
                        sound1.Open(new Uri(@"C:\Users\Public\Documents\kaya17\bin\one-minute-remaining.mp3"));
                        sound1.Play();
                    }
                    else
                    {
                        AutoClosingMessageBox.Show(remaining + " minutes remaining", "Incubating", 1000);
                    }

                    Task.Delay(60000).Wait();
                }

                incubationRemaining_tb.Text = 0.ToString();
                AutoClosingMessageBox.Show("Incubation Completed", "Incubation", 1000);

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for probe to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for probe to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult2 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult2 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult2 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change Probe Draining Box and Cartridge to finished color
                probeDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                probeDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -----------------------------------------
                // ** Probe Incubation and Drain Complete **

                // ** HBSS Dispense **
                // -------------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to HBSS_Bottle
                moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Draw 1188 steps (2.2mL)
                drawLiquid(1188);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                // Change HBSS Dispense Box to in progress color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing HBSS", "Dispensing", 1000);

                //**E2**//
                // Move from HBSS bottle to E2
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.HBSS_Bottle]);
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.HBSS_Bottle]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing HBSS in E2", "Dispensing", 1000);

                // Dispense 400ul HBSS in E2
                dispenseLiquid(216);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense HBSS in remaining wells
                for (int i = 15; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Before dispensing in A3, go back to HBSS bottle and draw more
                    if (i == 19)
                    {
                        // Move from A3 to HBSS bottle
                        moveX(xPos[(int)steppingPositions.HBSS_Bottle] - xPos[(int)steppingPositions.A3]);
                        moveY(yPos[(int)steppingPositions.HBSS_Bottle] - yPos[(int)steppingPositions.A3]);

                        // Dispense remaining liquid before drawing more
                        dispenseLiquid(200);

                        // Draw HBSS for rows 3 and 4
                        AutoClosingMessageBox.Show("Drawing HBSS", "Drawing HBSS", 1000);
                        File.AppendAllText(logFilePath, "Drawing HBSS" + Environment.NewLine);

                        // Lower z position by 9800 steps
                        lowerZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Draw 2200 steps (4mL + extra)
                        drawLiquid(2200);

                        // Raise by 9800 steps
                        raiseZPosition(zPos[(int)steppingPositions.HBSS_Bottle]);

                        // Move back to A3
                        moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.HBSS_Bottle]);
                        moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.HBSS_Bottle]);
                    }

                    AutoClosingMessageBox.Show("Dispensing HBSS in " + positions[i], "Dispensing", 1000);

                    // dispense remaining liquid in last well
                    if (i == 28)
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);

                        // wait 3 seconds and dispense remaining amount
                        Task.Delay(3000).Wait();

                        dispenseLiquid(100);
                    }
                    else
                    {
                        // Dispense 400ul HBSS
                        dispenseLiquid(216);
                    }

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change HBSS Dispense box to finished color
                hbssDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("HBSS Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "HBSS Dispensed" + Environment.NewLine);

                // ----------------------------
                // ** HBSS Dispense Complete **

                // ** HBSS Wash **
                // ----------------

                // Change wells to gray
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Pipette Tip", "Cleaning", 1000);

                // Move from A4 to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.A4]);

                // Lower pipette tip
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2430 steps (4.5mL)
                drawLiquid(2430);

                // Raise pipette tip
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.5mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ------------------------
                // ** HBSS Wash Complete **

                // ** HBSS Drain **
                // ----------------

                // Change HBSS Draining Box and Cartridge to in progress color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                hbssDrain_tb.Foreground = Brushes.Black;
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Wash_Bottle]);

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                AutoClosingMessageBox.Show("Wait " + drainMinutes + " minutes for HBSS to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait " + drainMinutes + " minutes for HBSS to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 8);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    // Send signal to turn off pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                    // Switch back to bank 1
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                MessageBoxResult messageBoxResult3 = MessageBox.Show("Would you like to drain for 1 more minute?", "Add Drain Time", MessageBoxButton.YesNo);

                if (messageBoxResult3 == MessageBoxResult.Yes)
                {
                    // Turn pump on
                    try
                    {
                        // Switch to bank 2
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 1);
                        }
                        // Send signal to turn on pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    // Leave pump on for 1 minute
                    Task.Delay(60000).Wait();

                    // Turn pump off
                    try
                    {
                        // Send signal to turn off pump
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                        // Switch back to bank 1
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(switchBanks, "port2",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                else if (messageBoxResult3 == MessageBoxResult.No)
                {
                    AutoClosingMessageBox.Show("Continuing", "Continuing", 1000);
                }

                // Change HBSS Draining Box and Cartridge to finished color
                hbssDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                hbssDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 4; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // -------------------------
                // ** HBSS Drain Complete **

                // ** RB Dispense **
                // -----------------

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Draw 1145 steps (2.1mL + extra)
                drawLiquid(1145);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Change RB Dispense Box to in progress color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                // Change wells back to gray
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Dispensing RB", "Dispensing", 1000);

                //**E4**//
                // Move from RB bottle to E4
                moveY(yPos[(int)steppingPositions.E4] - yPos[(int)steppingPositions.RB_Bottle]);
                moveX(xPos[(int)steppingPositions.E4] - xPos[(int)steppingPositions.RB_Bottle]);

                // Change E4 to in progress color
                inProgressE4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in E4", "Dispensing", 1000);

                // Dispense 420ul RB in E4
                dispenseLiquid(227);

                // Change E4 to finished color
                inProgressE4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Dispense RB in remaining wells
                for (int i = 25; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Dispensing RB in " + positions[i], "Dispensing", 1000);

                    dispenseLiquid(227);

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                // Change RB Dispense box to finished color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                AutoClosingMessageBox.Show("Read Buffer Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "Read Buffer Dispensed" + Environment.NewLine);

                // --------------------------
                // ** RB Dispense Complete **

                // ** RB Wash **
                // -------------

                // Change Cartridges to gray
                for (int i = 14; i < 19; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move from A4 to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.A4]);

                // Dispense remaining RB in bottle
                dispenseLiquid(200);

                // Move from RB_Bottle to Wash_Bottle
                moveX(xPos[(int)steppingPositions.Wash_Bottle] - xPos[(int)steppingPositions.RB_Bottle]);
                moveY(yPos[(int)steppingPositions.Wash_Bottle] - yPos[(int)steppingPositions.RB_Bottle]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Draw 2268 steps (4.2mL)
                drawLiquid(2268);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.Wash_Bottle]);

                // Dispense 2500 steps (4.2mL + extra)
                dispenseLiquid(2500);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // ----------------------
                // ** RB Wash Complete **

                // ** Read Wells **
                // ----------------

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                // Board Initialization
                AutoClosingMessageBox.Show("Initializing Reader Board", "Initializing", 1000);
                File.AppendAllText(logFilePath, "Initializing Reader Board");

                StringBuilder sb = new StringBuilder(5000);
                bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

                MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());
                File.AppendAllText(logFilePath, "Test initializeBoard: " + initializeBoardBool + Environment.NewLine);

                // Define headers for all columns in output file
                string outputFileDataHeaders = "WellName" + delimiter + "Sample Name" + delimiter + "RFU" + delimiter + "TestResult" + delimiter + Environment.NewLine;

                // Add headers to output file
                File.AppendAllText(outputFilePath, outputFileDataHeaders);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_cart34.Visibility = Visibility.Hidden;
                inProgress_cart34_border.Visibility = Visibility.Hidden;

                File.AppendAllText(logFilePath, "In progress boxes hidden" + Environment.NewLine);

                File.AppendAllText(logFilePath, "Results grid visible" + Environment.NewLine);

                resultsDisplay_cart34_border.Visibility = Visibility.Visible;
                resultsDisplay_cart34.Visibility = Visibility.Visible;

                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // Move from Wash_Bottle to E4 + Dispense_to_Read
                moveX((xPos[(int)steppingPositions.E4] + xPos[(int)steppingPositions.Dispense_to_Read]) - xPos[(int)steppingPositions.Wash_Bottle]);
                moveY((yPos[(int)steppingPositions.E4] + yPos[(int)steppingPositions.Dispense_to_Read]) - yPos[(int)steppingPositions.Wash_Bottle]);

                inProgressEllipses[14].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                resultsTextboxes[14].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading E4", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading E4" + Environment.NewLine);

                StringBuilder sbE4 = new StringBuilder(100000);

                IntPtr testBoardValuePtrE4 = testGetBoardValue(sbE4, sbE4.Capacity, excitationLedVoltage);
                double[] testArrayE4 = new double[5];
                Marshal.Copy(testBoardValuePtrE4, testArrayE4, 0, 5);
                string inputStringE4 = "";
                inputStringE4 += "Return Value: m_dAvgValue = " + testArrayE4[0] + "\n";
                inputStringE4 += "Return Value: m_dCumSum = " + testArrayE4[1] + "\n";
                inputStringE4 += "Return Value: m_dLEDtmp = " + testArrayE4[2] + "\n";
                inputStringE4 += "Return Value: m_dPDtmp = " + testArrayE4[3] + "\n";
                inputStringE4 += "Return Value: testGetBoardValue = " + testArrayE4[4] + "\n";

                File.AppendAllText(logFilePath, "E4 GetBoardValue: " + inputStringE4 + Environment.NewLine);

                if (testArrayE4[4] == 0)
                {
                    MessageBox.Show("Error reading E4. Check log for specifics");
                }

                inProgressEllipses[14].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                resultsTextboxes[14].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayE4[0] - shiftFactors[(int)shiftsAndScales.E4]) * scaleFactors[(int)shiftsAndScales.E4];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.E4];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.E4],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                // Add results grid data for E4 to output file
                outputFileData = positions[(int)steppingPositions.E4] + delimiter + resultsTextboxes[14].Text +
                                            delimiter + raw_avg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[14].Text = raw_avg.ToString();
                resultsTextboxes[14].Foreground = Brushes.Black;

                // Loop through rest of reading steps
                for (int i = 25; i < 29; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    AutoClosingMessageBox.Show("Reading " + positions[i], "Reading", 2000);
                    File.AppendAllText(logFilePath, "Reading " + positions[i] + Environment.NewLine);

                    // Read sample in current well
                    StringBuilder sbR = new StringBuilder(100000);

                    IntPtr boardValuePtr = testGetBoardValue(sbR, sbR.Capacity, excitationLedVoltage);
                    double[] readingValues = new double[5];
                    Marshal.Copy(boardValuePtr, readingValues, 0, 5);
                    string readingInputString = "";
                    readingInputString += "Return Value: m_dAvgValue = " + readingValues[0] + "\n";
                    readingInputString += "Return Value: m_dCumSum = " + readingValues[1] + "\n";
                    readingInputString += "Return Value: m_dLEDtmp = " + readingValues[2] + "\n";
                    readingInputString += "Return Value: m_dPDtmp = " + readingValues[3] + "\n";
                    readingInputString += "Return Value: testGetBoardValue = " + readingValues[4] + "\n";

                    File.AppendAllText(logFilePath, positions[i] + " GetBoardValue: " + readingInputString + Environment.NewLine);

                    if (readingValues[4] == 0)
                    {
                        MessageBox.Show("Error reading " + positions[i] + ". Check log for specifics.");
                    }

                    // Update Results Grid for current well
                    raw_avg = (readingValues[0] - shiftFactors[i - 10]) * scaleFactors[i - 10];
                    raw_avg = Math.Round(raw_avg, 3);

                    TC_rdg = raw_avg;

                    diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                    testResult = "NEG";

                    if (diff > threshold)
                    {
                        testResult = "POS";
                    }

                    sampleName = positions[i];

                    testResults.Add(new TestResults()
                    {
                        WellName = positions[i],
                        BackgroundReading = bgd_rdg.ToString(),
                        Threshold = threshold.ToString(),
                        RawAvg = raw_avg.ToString(),
                        TC_rdg = TC_rdg.ToString(),
                        TestResult = testResult,
                        SampleName = sampleName
                    });

                    // Add results grid data for current well to output file
                    outputFileData = positions[i] + delimiter + resultsTextboxes[i - 10].Text +
                                            delimiter + raw_avg.ToString() + delimiter + testResult + Environment.NewLine;

                    File.AppendAllText(outputFilePath, outputFileData);

                    resultsTextboxes[i - 10].Text = raw_avg.ToString();
                    resultsTextboxes[i - 10].Foreground = Brushes.Black;

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 28)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        File.AppendAllText(dataFilePath, sbR.ToString() + Environment.NewLine);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        resultsTextboxes[i - 10].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                        resultsTextboxes[i - 9].Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                }

                AutoClosingMessageBox.Show("All wells read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All wells read" + Environment.NewLine);

                testCloseTasksAndChannels();

                // ----------------------------
                // ** Reading Wells Complete **

                // ** Move Back to Load **
                // -----------------------

                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.A4] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.A4] + yPos[(int)steppingPositions.Dispense_to_Read]));

                AutoClosingMessageBox.Show("Reading complete", "Results", 2000);
                File.AppendAllText(logFilePath, "Reading complete" + Environment.NewLine);

                isReading = false;
                File.AppendAllText(logFilePath, "Reading completed" + Environment.NewLine);

                // --------------
                // ** Complete **
            }

            else
            {
                MessageBox.Show("Please enter a valid row number or 234 for 15 wells.");
            }
        }

        private void moveX(int v)
        {
            // If negative, move x negative
            if (v < 0)
            {
                for (int i = 0; i < Math.Abs(v); i++)
                {
                    try
                    {
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 16);
                            Thread.Sleep(wait);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            // If positive, move x positive
            else
            {
                for (int i = 0; i < Math.Abs(v); i++)
                {
                    try
                    {
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 32);
                            Thread.Sleep(wait);
                            writer.WriteSingleSamplePort(true, 48);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void moveY(int v)
        {
            // If negative, move y negative
            if (v < 0)
            {
                for (int i = 0; i < Math.Abs(v); i++)
                {
                    try
                    {
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 4);
                            Thread.Sleep(wait);
                            writer.WriteSingleSamplePort(true, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            // If positive, move y positive
            else
            {
                for (int i = 0; i < Math.Abs(v); i++)
                {
                    try
                    {
                        using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                        {
                            //  Create an Digital Output channel and name it.
                            digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                ChannelLineGrouping.OneChannelForAllLines);

                            //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                            //  of digital data on demand, so no timeout is necessary.
                            DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                            writer.WriteSingleSamplePort(true, 12);
                            Thread.Sleep(wait);
                            writer.WriteSingleSamplePort(true, 8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        class AutoClosingMessageBox
        {
            System.Threading.Timer _timeoutTimer;
            string _caption;
            AutoClosingMessageBox(string text, string caption, int timeout)
            {
                _caption = caption;
                _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                    null, timeout, System.Threading.Timeout.Infinite);
                MessageBox.Show(text, caption);
            }
            public static void Show(string text, string caption, int timeout)
            {
                new AutoClosingMessageBox(text, caption, timeout);
            }
            void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow(null, _caption);
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }
            const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }

        private void drawLiquid(int v)
        {
            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                        Thread.Sleep(wait);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void dispenseLiquid(int v)
        {
            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 3);
                        Thread.Sleep(wait);
                        writer.WriteSingleSamplePort(true, 2);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void raiseZPosition(int v)
        {
            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 192);
                        //Thread.Sleep(1);
                        writer.WriteSingleSamplePort(true, 128);
                    }

                    try
                    {
                        using (NationalInstruments.DAQmx.Task digitalReadTask = new NationalInstruments.DAQmx.Task())
                        {
                            digitalReadTask.DIChannels.CreateChannel(
                                readLimSwitches,
                                "port1",
                                ChannelLineGrouping.OneChannelForAllLines);

                            DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                            UInt32 data = reader.ReadSingleSamplePortUInt32();

                            //Update the Data Read box
                            string limitInputText = data.ToString();

                            if (limitInputText == "4")
                            {
                                break;
                            }
                        }
                    }
                    catch (DaqException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            try
            {
                using (NationalInstruments.DAQmx.Task digitalReadTask = new NationalInstruments.DAQmx.Task())
                {
                    digitalReadTask.DIChannels.CreateChannel(
                        readLimSwitches,
                        "port1",
                        ChannelLineGrouping.OneChannelForAllLines);

                    DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                    UInt32 data = reader.ReadSingleSamplePortUInt32();

                    //Update the Data Read box
                    string limitInputText = data.ToString();

                    if (limitInputText == "4")
                    {
                        for (int i = 0; i < 300; i++)
                        {
                            try
                            {
                                using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                                {
                                    //  Create an Digital Output channel and name it.
                                    digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                                        ChannelLineGrouping.OneChannelForAllLines);

                                    //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                                    //  of digital data on demand, so no timeout is necessary.
                                    DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                                    writer.WriteSingleSamplePort(true, 64);
                                    Thread.Sleep(1);
                                    writer.WriteSingleSamplePort(true, 0);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void lowerZPosition(int v)
        {
            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(writeAllSteps, "port0",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 64);
                        //Thread.Sleep(1);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void quit_button_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("Closing");
            //MessageBox.Show("Closing");
            //Application.Current.Shutdown();

            if (isReading == true)
            {
                MessageBoxResult mbResult = MessageBox.Show("Reading not complete. Are you sure you want to quit application?", "Quit", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
                else if (mbResult == MessageBoxResult.No)
                {

                }
            }

            else
            {
                MessageBoxResult mbResult = MessageBox.Show("Are you sure you want to quit application?", "Quit", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
                else if (mbResult == MessageBoxResult.No)
                {

                }
            }
        }

        private void readTest_btn_Click(object sender, RoutedEventArgs e)
        {
            double[] testArray = new double[17];

            // read parameter file and read in all necessary parameters
            string[] parameters = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\Kaya17Covi2V.txt");
            double ledOutputRange = double.Parse(parameters[0].Substring(0, 3));
            double numSamplesPerReading = double.Parse(parameters[1].Substring(0, 3));
            double numTempSamplesPerReading = double.Parse(parameters[2].Substring(0, 2));
            double samplingRate = double.Parse(parameters[3].Substring(0, 5));
            double numSamplesForAvg = double.Parse(parameters[4].Substring(0, 3));
            double errorLimitInMillivolts = double.Parse(parameters[5].Substring(0, 2));
            double saturation = double.Parse(parameters[8].Substring(0, 8));
            double expectedDarkRdg = double.Parse(parameters[6].Substring(0, 4));
            double lowSignal = double.Parse(parameters[29].Substring(0, 5));
            double readMethod = double.Parse(parameters[9].Substring(0, 1));
            double ledOnDuration = double.Parse(parameters[9].Substring(4, 3));
            double readDelayInMS = double.Parse(parameters[9].Substring(8, 1));
            double excitationLedVoltage = double.Parse(parameters[33].Substring(0, 5));
            double excMinVoltage = double.Parse(parameters[26].Substring(0, 3));
            double excNomVoltage = double.Parse(parameters[25].Substring(0, 4));
            double excMaxVoltage = double.Parse(parameters[24].Substring(0, 3));
            double calMinVoltage = double.Parse(parameters[18].Substring(0, 3));
            double calNomVoltage = double.Parse(parameters[17].Substring(0, 3));
            double calMaxVoltage = double.Parse(parameters[16].Substring(0, 3));



            testArray[0] = ledOutputRange;
            testArray[1] = samplingRate;
            testArray[2] = numSamplesPerReading;
            testArray[3] = numSamplesForAvg;
            testArray[4] = errorLimitInMillivolts;
            testArray[5] = numTempSamplesPerReading;
            testArray[6] = saturation;
            testArray[7] = expectedDarkRdg;
            testArray[8] = lowSignal;
            testArray[9] = ledOnDuration;
            testArray[10] = readDelayInMS;
            testArray[11] = calMinVoltage;
            testArray[12] = calNomVoltage;
            testArray[13] = calMaxVoltage;
            testArray[14] = excMinVoltage;
            testArray[15] = excNomVoltage;
            testArray[16] = excMaxVoltage;

            string testString = "";
            foreach (double value in testArray)
            {
                testString += "Input: " + value + "\n";
            }

            MessageBox.Show(testString);

            IntPtr testPtr = verifyInput(testArray);
            double[] testArray2 = new double[17];
            Marshal.Copy(testPtr, testArray2, 0, 17);
            testString = "";
            foreach (double value in testArray2)
            {
                testString += "Verify input: " + value + "\n";
            }

            MessageBox.Show(testString);

            bool settingsBool = testSetSettings(testArray);

            MessageBox.Show("Test setSettings: " + settingsBool);

            double[] avgVals = new double[5];

            StringBuilder sb = new StringBuilder(5000);
            bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

            MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());

            for (int i = 0; i < 4; i++)
            {
                MessageBox.Show("Insert Cartridge and then click ok");

                StringBuilder sb2 = new StringBuilder(10000);

                IntPtr testBoardValuePtr = testGetBoardValue(sb2, sb2.Capacity, excitationLedVoltage);
                double[] testArray3 = new double[5];
                Marshal.Copy(testBoardValuePtr, testArray3, 0, 5);
                testString = "";
                testString += "Return Value: m_dAvgValue = " + testArray3[0] + "\n";
                // avgVals.Append(testArray3[0]);
                avgVals[i] = testArray3[0];
                testString += "Return Value: m_dCumSum = " + testArray3[1] + "\n";
                testString += "Return Value: m_dLEDtmp = " + testArray3[2] + "\n";
                testString += "Return Value: m_dPDtmp = " + testArray3[3] + "\n";
                testString += "Return Value: testGetBoardValue = " + testArray3[4] + "\n";

                MessageBox.Show(testString + sb2.ToString());

                Console.WriteLine(avgVals[i].ToString());
            }

            testCloseTasksAndChannels();
        }

        private void musicBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer sound1 = new MediaPlayer();
            sound1.Open(new Uri(@"C:\Users\seaot\Downloads\43536__mkoenig__ultra-dnb-loop-160bpm.wav"));
            sound1.Play();
        }
    }
}