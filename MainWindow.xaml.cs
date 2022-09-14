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

namespace AutoVega4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //bool flipX;
        //bool flipY;
        bool isReading = false;
        readonly string logFilePath;
        readonly string outputFilePath;
        readonly string timeStamp;
        readonly string testTime;
        readonly string writeAllSteps;
        readonly string readLimSwitches;
        readonly string switchBanks;
        readonly string delimiter;
        readonly string[] map;
        string testResult;
        string sampleName;
        string outputFileData;
        readonly int drainTime = 120000; //wait for 2 minutes
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
        static extern IntPtr testGetBoardValue(StringBuilder str, int len);

        [DllImport(@".\DLLs\DAQinterfaceForKaya17.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void testCloseTasksAndChannels();

        public MainWindow()
        {
            Directory.CreateDirectory(@"C:\Users\Public\Documents\kaya17\log");
            Directory.CreateDirectory(@"C:\Users\Public\Documents\kaya17\data");

            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            timeStamp = DateTime.Now.ToString("ddMMMyy_HHmmss");
            testTime = DateTime.Now.ToString("ddMMM_HHmm");

            logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega4_logfile.txt";
            outputFilePath = @"C:\Users\Public\Documents\Kaya17\Data\kaya17-AutoVega4_" + timeStamp + ".csv";

            writeAllSteps = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[3];
            readLimSwitches = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[4];
            switchBanks = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[5];

            map = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\34_well_cartridge_steps_1row_test.csv");

            delimiter = ",";

            isReading = false;

            testResult = "NEG";

            InitializeComponent();
        }
        
        private void operator_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
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

                if (Allergy_rb != null && Allergy_rb.IsEnabled == false)
                {
                    Allergy_rb.IsEnabled = true;
                }

                if (Ovarian_rb != null && Ovarian_rb.IsEnabled == false)
                {
                    Ovarian_rb.IsEnabled = true;
                }
            }
            catch { }

            testname_tb.IsReadOnly = true;
            testname_tb.Text = "Kaya17-AutoVega_" + testTime;
            testname_tb.FontSize = 16;

            File.AppendAllText(logFilePath, "Test type radiobuttons enabled" + Environment.NewLine);
        }

        private async void start_button_Click(object sender, RoutedEventArgs e)
        {
            string appendOutputHeaders = "Operator ID" + delimiter + "Kit ID" + delimiter +
                "Reader ID" + delimiter + "Test Type" + delimiter + Environment.NewLine;
            try
            {
                File.WriteAllText(outputFilePath, appendOutputHeaders);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //MessageBox.Show("File headers written");
            File.AppendAllText(logFilePath, "File headers written" + Environment.NewLine);

            string operator_ID = operator_tb.Text;
            string kit_ID = kit_tb.Text;
            string reader_ID = reader_tb.Text;

            //MessageBox.Show("IDs set");
            File.AppendAllText(logFilePath, "IDs set" + Environment.NewLine);

            if ((bool)Covid19_rb.IsChecked == true)
            {
                string test = "Covid19";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)Allergy_rb.IsChecked == true)
            {
                string test = "Allergy";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)Ovarian_rb.IsChecked == true)
            {
                string test = "Ovarian Panel";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
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

            MessageBox.Show("Open lid, load cartridge(s), close lid, enter patient name and number of samples," +
                " and press 'read array cartridge' after cartridge is aligned.");

            // **TODO: Check for cartridge alignment**

            // enable patient info, sample number, and read button
            patient_tb.IsEnabled = true;
            sampleNum_tb.IsEnabled = true;
            read_button.IsEnabled = true;

            File.AppendAllText(logFilePath, "Patient info, sample number, and read button enabled" + Environment.NewLine);

            // disable operator, kit, reader, radiobuttons
            operator_tb.IsEnabled = false;
            kit_tb.IsEnabled = false;
            reader_tb.IsEnabled = false;
            Covid19_rb.IsEnabled = false;
            Allergy_rb.IsEnabled = false;
            Ovarian_rb.IsEnabled = false;

            File.AppendAllText(logFilePath, "Operator id, kit id, reader id, and test type radiobuttons disabled" + Environment.NewLine);
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
            Probe_Wash_Bottle = 7,
            RB_Wash_Bottle = 8,
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

        private void MoveToHomePosition()
        {
            string[] map = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\34_well_cartridge_steps.csv");
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

            // turn z motor until limit switch is reached (z positive)
            for (int i = 0; i < zPos[(int)steppingPositions.Home]; i++)
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
                        Thread.Sleep(wait);
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
                            string LimitInputText = data.ToString();

                            if (LimitInputText == "4" || LimitInputText == "5" || LimitInputText == "6" || LimitInputText == "7")
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

            // steps z back so limit switch is not pressed
            for (int i = 0; i < zPos[(int)steppingPositions.Back_Off]; i++)
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
                        Thread.Sleep(wait);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            // turns motor a specified amount of steps (x negative)
            for (int i = 0; i < xPos[(int)steppingPositions.Home]; i++)
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
                            string LimitInputText = data.ToString();

                            if (LimitInputText == "1" || LimitInputText == "3" || LimitInputText == "5" || LimitInputText == "7")
                            {
                                //MessageBox.Show("X limit reached");
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

            // steps x forward so limit switch is not pressed
            for (int i = 0; i < xPos[(int)steppingPositions.Back_Off]; i++)
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

            // turns motor a specified amount of steps (y negative)
            for (int i = 0; i < yPos[(int)steppingPositions.Home]; i++)
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
                            string LimitInputText = data.ToString();

                            if (LimitInputText == "2" || LimitInputText == "3" || LimitInputText == "6" || LimitInputText == "7")
                            {
                                //MessageBox.Show("Y limit reached");
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

            // steps y forward so limit switch is not pressed
            for (int i = 0; i < yPos[(int)steppingPositions.Back_Off]; i++)
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

        private void MoveToLoadPosition()
        {
            string[] map = File.ReadAllLines("../../Auto Vega/34_well_cartridge_steps.csv");
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
            if (sampleNum_tb.Text == "30")
            {
                string inProgressColor = "#F5D5CB";
                string finishedColor = "#D7ECD9";

                isReading = true;
                File.AppendAllText(logFilePath, "Reading started" + Environment.NewLine);
                List<TestResults> testResults = new List<TestResults>();

                // define array for necessary items from parameter file
                double[] testArray = new double[17];

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

                //MessageBox.Show(inputString);
                File.AppendAllText(logFilePath, inputString + Environment.NewLine);

                IntPtr testPtr = verifyInput(testArray);
                double[] testArray2 = new double[17];
                Marshal.Copy(testPtr, testArray2, 0, 17);
                inputString = "";
                foreach (double value in testArray)
                {
                    inputString += "Verify input: " + value + "\n";
                }

                //MessageBox.Show(inputString);
                File.AppendAllText(logFilePath, inputString + Environment.NewLine);

                bool settingsBool = testSetSettings(testArray);

                //MessageBox.Show("Test setSettings: " + settingsBool);
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

                // ** Start of moving steps **
                // ---------------------------

                // Wait 20 mins
                AutoClosingMessageBox.Show("Incubating for 20 minutes", "Incubating", 3000);
                //Task.Delay(1200000).Wait();
                Task.Delay(1000).Wait(); // 1 second instead of 30 mins

                //MessageBox.Show("Moving to Drain Position");
                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Load]);
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Load]);

                // Change Sample Draining Box and Cartridge to in progress color
                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Foreground = Brushes.Black;
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                //MessageBox.Show("Wait 2 minutes for samples to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 2 minutes for samples to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait 2 minutes for samples to drain through cartridges" + Environment.NewLine);

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

                // Leave pump on for 2 minutes
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

                // Change Sample Draining Box and Cartridge to finished color
                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                //MessageBox.Show("Sample Draining Complete");
                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move to Probe Bottle
                moveX(xPos[(int)steppingPositions.Probe_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.Probe_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Draw Probe for eight wells (A1, B1, A2, A3, A4, A5, A6, A7)
                AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                // Draw 1848 steps (2.64mL)
                drawLiquid(1848);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                // Change Probe Dispense Box to in progress color
                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Foreground = Brushes.Black;

                //**A1**//
                // Move from probe bottle to A1
                moveY(yPos[(int)steppingPositions.A1] - yPos[(int)steppingPositions.Probe_Bottle]);
                moveX(xPos[(int)steppingPositions.A1] - xPos[(int)steppingPositions.Probe_Bottle]);

                // Change A1 to in progress color
                inProgressA1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A1", "Dispensing", 1000);

                // Dispense 330ul Probe in A1
                dispenseLiquid(231);

                // Change A1 to finished color
                inProgressA1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**B1**//
                // Move from A1 to B1
                moveX(xPos[(int)steppingPositions.B1] - xPos[(int)steppingPositions.A1]);
                moveY(yPos[(int)steppingPositions.B1] - yPos[(int)steppingPositions.A1]);

                // Change B1 to in progress color
                inProgressB1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in B1", "Dispensing", 1000);

                // Dispense 330ul Probe in B1
                dispenseLiquid(231);

                // Change B1 to finished color
                inProgressB1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A2**//
                // Move from B1 to A2
                moveX(xPos[(int)steppingPositions.A2] - xPos[(int)steppingPositions.B1]);
                moveY(yPos[(int)steppingPositions.A2] - yPos[(int)steppingPositions.B1]);

                // Change A2 to in progress color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A2", "Dispensing", 1000);

                // Dispense 330ul Probe in A2
                dispenseLiquid(231);

                // Change A2 to finished color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A3**//
                // Move from A2 to A3
                moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.A2]);

                // Change A3 to in progress color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A3", "Dispensing", 1000);

                // Dispense 330ul Probe in A3
                dispenseLiquid(231);

                // Change A3 to finished color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A4**//
                // Move from A3 to A4
                moveX(xPos[(int)steppingPositions.A4] - xPos[(int)steppingPositions.A3]);
                moveY(yPos[(int)steppingPositions.A4] - yPos[(int)steppingPositions.A3]);

                // Change A4 to in progress color
                inProgressA4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A4", "Dispensing", 1000);

                // Dispense 330ul Probe in A4
                dispenseLiquid(231);

                // Change A4 to finished color
                inProgressA4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A5**//
                // Move from A4 to A5
                moveX(xPos[(int)steppingPositions.A5] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.A5] - yPos[(int)steppingPositions.A4]);

                // Change A5 to in progress color
                inProgressA5.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A5", "Dispensing", 1000);

                // Dispense 330ul Probe in A5
                dispenseLiquid(231);

                // Change A5 to finished color
                inProgressA5.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A6**//
                // Move from A5 to A6
                moveX(xPos[(int)steppingPositions.A6] - xPos[(int)steppingPositions.A5]);
                moveY(yPos[(int)steppingPositions.A6] - yPos[(int)steppingPositions.A5]);

                // Change A6 to in progress color
                inProgressA6.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A6", "Dispensing", 1000);

                // Dispense 330ul Probe in A6
                dispenseLiquid(231);

                // Change A6 to finished color
                inProgressA6.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A7**//
                // Move from A6 to A7
                moveX(xPos[(int)steppingPositions.A7] - xPos[(int)steppingPositions.A6]);
                moveY(yPos[(int)steppingPositions.A7] - yPos[(int)steppingPositions.A6]);

                // Change A7 to in progress color
                inProgressA7.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A7", "Dispensing", 1000);

                // Dispense 330ul Probe in A7
                dispenseLiquid(331);

                // Change A7 to finished color
                inProgressA7.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change Probe Dispense box to finished color
                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("Probe Dispensed");
                AutoClosingMessageBox.Show("Probe Dispensed", "Dispensing Complete", 1000);
                File.AppendAllText(logFilePath, "Probe Dispensed" + Environment.NewLine);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                AutoClosingMessageBox.Show("Cleaning Probe Tip", "Cleaning", 1000);

                // Move from A7 to Probe_Wash_Bottle
                moveX(xPos[(int)steppingPositions.Probe_Wash_Bottle] - xPos[(int)steppingPositions.A7]);
                moveY(yPos[(int)steppingPositions.Probe_Wash_Bottle] - yPos[(int)steppingPositions.A7]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.Probe_Wash_Bottle]);

                // Draw 2100 steps (3mL)
                drawLiquid(2100);
                
                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.Probe_Wash_Bottle]);

                // Dispense 2200 steps (3mL + extra)
                dispenseLiquid(2300);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // Wait 20 mins
                AutoClosingMessageBox.Show("Incubating for 20 minutes", "Incubating", 3000);
                //Task.Delay(1200000).Wait();
                Task.Delay(1000).Wait(); // 1 second instead of 20 mins

                //MessageBox.Show("Moving Back to Drain Position");
                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 1000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.Probe_Wash_Bottle]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.Probe_Wash_Bottle]);

                // Change WB Draining Box and Cartridge to in progress color
                wbDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_tb.Foreground = Brushes.Black;
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Lower Pipette Tips to Drain
                lowerZPosition(zPos[(int)steppingPositions.Drain]);

                //MessageBox.Show("Wait 2 minutes for WB to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 2 minutes for WB to drain through cartridges", "Draining", 1000);
                File.AppendAllText(logFilePath, "Wait 2 minutes for WB to drain through cartridges" + Environment.NewLine);

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

                // Leave pump on for 2 minutes
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

                // Change WB Draining Box and Cartridge to finished color
                wbDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                //MessageBox.Show("Draining Complete");
                AutoClosingMessageBox.Show("Draining complete", "Draining Complete", 1000);
                File.AppendAllText(logFilePath, "Draining complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move from drain to RB_Bottle
                moveX(xPos[(int)steppingPositions.RB_Bottle] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.RB_Bottle] - yPos[(int)steppingPositions.Drain]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Draw 2030 steps (2.9mL)
                drawLiquid(2030);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle]);

                // Change RB Dispense Box to in progress color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                //**A1**//
                // Move from RB bottle to A1
                moveY(yPos[(int)steppingPositions.A1] - yPos[(int)steppingPositions.RB_Bottle]);
                moveX(xPos[(int)steppingPositions.A1] - xPos[(int)steppingPositions.RB_Bottle]);

                // Change A1 to in progress color
                inProgressA1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A1", "Dispensing", 1000);

                // Dispense 350ul RB in A1
                dispenseLiquid(245);

                // Change A1 to finished color
                inProgressA1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**B1**//
                // Move from A1 to B1
                moveX(xPos[(int)steppingPositions.B1] - xPos[(int)steppingPositions.A1]);
                moveY(yPos[(int)steppingPositions.B1] - yPos[(int)steppingPositions.A1]);

                // Change B1 to in progress color
                inProgressB1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in B1", "Dispensing", 1000);

                // Dispense 350ul RB in B1
                dispenseLiquid(245);

                // Change B1 to finished color
                inProgressB1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A2**//
                // Move from B1 to A2
                moveX(xPos[(int)steppingPositions.A2] - xPos[(int)steppingPositions.B1]);
                moveY(yPos[(int)steppingPositions.A2] - yPos[(int)steppingPositions.B1]);

                // Change A2 to in progress color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A2", "Dispensing", 1000);

                // Dispense 350ul RB in A2
                dispenseLiquid(245);

                // Change A2 to finished color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A3**//
                // Move from A2 to A3
                moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.A2]);

                // Change A3 to in progress color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A3", "Dispensing", 1000);

                // Dispense 350ul RB in A3
                dispenseLiquid(245);

                // Change A3 to finished color
                inProgressA3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A4**//
                // Move from A3 to A4
                moveX(xPos[(int)steppingPositions.A4] - xPos[(int)steppingPositions.A3]);
                moveY(yPos[(int)steppingPositions.A4] - yPos[(int)steppingPositions.A3]);

                // Change A4 to in progress color
                inProgressA4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A4", "Dispensing", 1000);

                // Dispense 350ul RB in A4
                dispenseLiquid(245);

                // Change A4 to finished color
                inProgressA4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A5**//
                // Move from A4 to A5
                moveX(xPos[(int)steppingPositions.A5] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.A5] - yPos[(int)steppingPositions.A4]);

                // Change A5 to in progress color
                inProgressA5.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A5", "Dispensing", 1000);

                // Dispense 350ul RB in A5
                dispenseLiquid(245);

                // Change A5 to finished color
                inProgressA5.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A6**//
                // Move from A5 to A6
                moveX(xPos[(int)steppingPositions.A6] - xPos[(int)steppingPositions.A5]);
                moveY(yPos[(int)steppingPositions.A6] - yPos[(int)steppingPositions.A5]);

                // Change A6 to in progress color
                inProgressA6.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A6", "Dispensing", 1000);

                // Dispense 350ul RB in A6
                dispenseLiquid(245);

                // Change A6 to finished color
                inProgressA6.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A7**//
                // Move from A6 to A7
                moveX(xPos[(int)steppingPositions.A7] - xPos[(int)steppingPositions.A6]);
                moveY(yPos[(int)steppingPositions.A7] - yPos[(int)steppingPositions.A6]);

                // Change A7 to in progress color
                inProgressA7.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A7", "Dispensing", 1000);

                // Dispense 350ul RB in A7
                dispenseLiquid(245);

                // Change A7 to finished color
                inProgressA7.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change RB Dispense box to finished color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("RB Dispensed");
                AutoClosingMessageBox.Show("Read Buffer Dispensed", "Dispensing Complete", 2000);
                MessageBox.Show("RB Dispensed");
                File.AppendAllText(logFilePath, "Read Buffer Dispensed" + Environment.NewLine);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Move from A7 to RB_Wash_Bottle
                moveX(xPos[(int)steppingPositions.RB_Wash_Bottle] - xPos[(int)steppingPositions.A7]);
                moveY(yPos[(int)steppingPositions.RB_Wash_Bottle] - yPos[(int)steppingPositions.A7]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Wash_Bottle]);

                // Draw 2100 steps (3mL)
                drawLiquid(2100);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Wash_Bottle]);

                // Dispense 2400 steps (3.2mL + extra)
                dispenseLiquid(2400);

                Task.Delay(5000).Wait();

                dispenseLiquid(300);

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                // Change Reading box to in progress color
                reading_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_tb.Foreground = Brushes.Black;

                // Board Initialization
                AutoClosingMessageBox.Show("Initializing Reader Board", "Initializing", 2000);
                File.AppendAllText(logFilePath, "Initializing Reader Board");

                StringBuilder sb = new StringBuilder(5000);
                bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

                MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());
                File.AppendAllText(logFilePath, "Test initializeBoard: " + initializeBoardBool + Environment.NewLine);

                // Define headers for all columns in output file
                string outputFileDataHeaders = "WellName" + delimiter + "BackgroundReading" + delimiter + "Threshold" + delimiter +
                                               "RawAvg" + delimiter + "TC_rdg" + delimiter + "TestResult" + Environment.NewLine;

                // Add headers to output file
                File.AppendAllText(outputFilePath, outputFileDataHeaders);

                //MessageBox.Show("Reading samples in all wells");
                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 2000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // A1 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from RB_Wash_Bottle to A1 + Dispense_to_Read
                moveY((yPos[(int)steppingPositions.A1] + yPos[(int)steppingPositions.Dispense_to_Read]) - yPos[(int)steppingPositions.RB_Wash_Bottle]);
                moveX((xPos[(int)steppingPositions.A1] + xPos[(int)steppingPositions.Dispense_to_Read]) - xPos[(int)steppingPositions.RB_Wash_Bottle]);

                // Change A1 to in progress color
                inProgressEllipses[0].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A1", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A1" + Environment.NewLine);

                // Read A1
                StringBuilder sbA1 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA1 = testGetBoardValue(sbA1, sbA1.Capacity);
                double[] testArrayA1 = new double[5];
                Marshal.Copy(testBoardValuePtrA1, testArrayA1, 0, 5);
                string inputStringA1 = "";
                inputStringA1 += "Return Value: m_dAvgValue = " + testArrayA1[0] + "\n";
                inputStringA1 += "Return Value: m_dCumSum = " + testArrayA1[1] + "\n";
                inputStringA1 += "Return Value: m_dLEDtmp = " + testArrayA1[2] + "\n";
                inputStringA1 += "Return Value: m_dPDtmp = " + testArrayA1[3] + "\n";
                inputStringA1 += "Return Value: testGetBoardValue = " + testArrayA1[4] + "\n";

                File.AppendAllText(logFilePath, "A1 avg value = " + testArrayA1[0] + Environment.NewLine);

                // Change A1 to finished color
                inProgressEllipses[0].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA1[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A1];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A1],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A1 added to results grid

                // Add results grid data for A1 to output file
                outputFileData = positions[(int)steppingPositions.A1] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[0].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A1 End

                // B1 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from A1 to B1
                moveX(xPos[(int)steppingPositions.B1] - xPos[(int)steppingPositions.A1]);
                moveY(yPos[(int)steppingPositions.B1] - yPos[(int)steppingPositions.A1]);

                // Change B1 to in progress color
                inProgressEllipses[1].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading B1", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading B1" + Environment.NewLine);

                // Read B1
                StringBuilder sbB1 = new StringBuilder(100000);

                IntPtr testBoardValuePtrB1 = testGetBoardValue(sbB1, sbB1.Capacity);
                double[] testArrayB1 = new double[5];
                Marshal.Copy(testBoardValuePtrB1, testArrayB1, 0, 5);
                string inputStringB1 = "";
                inputStringB1 += "Return Value: m_dAvgValue = " + testArrayB1[0] + "\n";
                inputStringB1 += "Return Value: m_dCumSum = " + testArrayB1[1] + "\n";
                inputStringB1 += "Return Value: m_dLEDtmp = " + testArrayB1[2] + "\n";
                inputStringB1 += "Return Value: m_dPDtmp = " + testArrayB1[3] + "\n";
                inputStringB1 += "Return Value: testGetBoardValue = " + testArrayB1[4] + "\n";

                File.AppendAllText(logFilePath, "B1 avg value = " + testArrayB1[0] + Environment.NewLine);

                // Change B1 to finished color
                inProgressEllipses[1].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayB1[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.B1];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.B1],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for B1 added to results grid

                // Add results grid data for B1 to output file
                outputFileData = positions[(int)steppingPositions.B1] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[1].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // B1 End

                // A2 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from B1 to A2
                moveX(xPos[(int)steppingPositions.A2] - xPos[(int)steppingPositions.B1]);
                moveY(yPos[(int)steppingPositions.A2] - yPos[(int)steppingPositions.B1]);

                // Change A2 to in progress color
                inProgressEllipses[8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A2", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A2" + Environment.NewLine);

                // Read A2
                StringBuilder sbA2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA2 = testGetBoardValue(sbA2, sbA2.Capacity);
                double[] testArrayA2 = new double[5];
                Marshal.Copy(testBoardValuePtrA2, testArrayA2, 0, 5);
                string inputStringA2 = "";
                inputStringA2 += "Return Value: m_dAvgValue = " + testArrayA2[0] + "\n";
                inputStringA2 += "Return Value: m_dCumSum = " + testArrayA2[1] + "\n";
                inputStringA2 += "Return Value: m_dLEDtmp = " + testArrayA2[2] + "\n";
                inputStringA2 += "Return Value: m_dPDtmp = " + testArrayA2[3] + "\n";
                inputStringA2 += "Return Value: testGetBoardValue = " + testArrayA2[4] + "\n";

                File.AppendAllText(logFilePath, "A2 avg value = " + testArrayA2[0] + Environment.NewLine);

                // Change A2 to finished color
                inProgressEllipses[8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA2[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A2];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A2],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A2 added to results grid

                // Add results grid data for A2 to output file
                outputFileData = positions[(int)steppingPositions.A2] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[8].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A2 End

                // A3 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from A2 to A3
                moveX(xPos[(int)steppingPositions.A3] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.A3] - yPos[(int)steppingPositions.A2]);

                // Change A3 to in progress color
                inProgressEllipses[9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A3", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A3" + Environment.NewLine);

                // Read A3
                StringBuilder sbA3 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA3 = testGetBoardValue(sbA3, sbA3.Capacity);
                double[] testArrayA3 = new double[5];
                Marshal.Copy(testBoardValuePtrA3, testArrayA3, 0, 5);
                string inputStringA3 = "";
                inputStringA3 += "Return Value: m_dAvgValue = " + testArrayA3[0] + "\n";
                inputStringA3 += "Return Value: m_dCumSum = " + testArrayA3[1] + "\n";
                inputStringA3 += "Return Value: m_dLEDtmp = " + testArrayA3[2] + "\n";
                inputStringA3 += "Return Value: m_dPDtmp = " + testArrayA3[3] + "\n";
                inputStringA3 += "Return Value: testGetBoardValue = " + testArrayA3[4] + "\n";

                File.AppendAllText(logFilePath, "A3 avg value = " + testArrayA3[0] + Environment.NewLine);

                // Change A3 to finished color
                inProgressEllipses[9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA3[0] - afeShiftFactor) * afeScaleFactor;
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

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A3 added to results grid

                // Add results grid data for A3 to output file
                outputFileData = positions[(int)steppingPositions.A3] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[9].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A3 End

                // A4 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from A3 to A4
                moveX(xPos[(int)steppingPositions.A4] - xPos[(int)steppingPositions.A3]);
                moveY(yPos[(int)steppingPositions.A4] - yPos[(int)steppingPositions.A3]);

                // Change A4 to in progress color
                inProgressEllipses[18].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A4", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A4" + Environment.NewLine);

                // Read A4
                StringBuilder sbA4 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA4 = testGetBoardValue(sbA4, sbA4.Capacity);
                double[] testArrayA4 = new double[5];
                Marshal.Copy(testBoardValuePtrA4, testArrayA4, 0, 5);
                string inputStringA4 = "";
                inputStringA4 += "Return Value: m_dAvgValue = " + testArrayA4[0] + "\n";
                inputStringA4 += "Return Value: m_dCumSum = " + testArrayA4[1] + "\n";
                inputStringA4 += "Return Value: m_dLEDtmp = " + testArrayA4[2] + "\n";
                inputStringA4 += "Return Value: m_dPDtmp = " + testArrayA4[3] + "\n";
                inputStringA4 += "Return Value: testGetBoardValue = " + testArrayA4[4] + "\n";

                File.AppendAllText(logFilePath, "A4 avg value = " + testArrayA4[0] + Environment.NewLine);

                // Change A4 to finished color
                inProgressEllipses[18].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA4[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A4];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A4],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A4 added to results grid

                // Add results grid data for A4 to output file
                outputFileData = positions[(int)steppingPositions.A4] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[18].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A4 End

                // A5 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from A4 to A5
                moveX(xPos[(int)steppingPositions.A5] - xPos[(int)steppingPositions.A4]);
                moveY(yPos[(int)steppingPositions.A5] - yPos[(int)steppingPositions.A4]);

                // Change A5 to in progress color
                inProgressEllipses[19].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A5", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A5" + Environment.NewLine);

                // Read A5
                StringBuilder sbA5 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA5 = testGetBoardValue(sbA5, sbA5.Capacity);
                double[] testArrayA5 = new double[5];
                Marshal.Copy(testBoardValuePtrA5, testArrayA5, 0, 5);
                string inputStringA5 = "";
                inputStringA5 += "Return Value: m_dAvgValue = " + testArrayA5[0] + "\n";
                inputStringA5 += "Return Value: m_dCumSum = " + testArrayA5[1] + "\n";
                inputStringA5 += "Return Value: m_dLEDtmp = " + testArrayA5[2] + "\n";
                inputStringA5 += "Return Value: m_dPDtmp = " + testArrayA5[3] + "\n";
                inputStringA5 += "Return Value: testGetBoardValue = " + testArrayA5[4] + "\n";

                File.AppendAllText(logFilePath, "A5 avg value = " + testArrayA5[0] + Environment.NewLine);

                // Change A5 to finished color
                inProgressEllipses[19].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA5[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A5];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A5],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A5 added to results grid

                // Add results grid data for A5 to output file
                outputFileData = positions[(int)steppingPositions.A5] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[19].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A5 End

                // A6 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from A5 to A6
                moveX(xPos[(int)steppingPositions.A6] - xPos[(int)steppingPositions.A5]);
                moveY(yPos[(int)steppingPositions.A6] - yPos[(int)steppingPositions.A5]);

                // Change A6 to in progress color
                inProgressEllipses[28].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A6", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A6" + Environment.NewLine);

                // Read A6
                StringBuilder sbA6 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA6 = testGetBoardValue(sbA6, sbA6.Capacity);
                double[] testArrayA6 = new double[5];
                Marshal.Copy(testBoardValuePtrA6, testArrayA6, 0, 5);
                string inputStringA6 = "";
                inputStringA6 += "Return Value: m_dAvgValue = " + testArrayA6[0] + "\n";
                inputStringA6 += "Return Value: m_dCumSum = " + testArrayA6[1] + "\n";
                inputStringA6 += "Return Value: m_dLEDtmp = " + testArrayA6[2] + "\n";
                inputStringA6 += "Return Value: m_dPDtmp = " + testArrayA6[3] + "\n";
                inputStringA6 += "Return Value: testGetBoardValue = " + testArrayA6[4] + "\n";

                File.AppendAllText(logFilePath, "A6 avg value = " + testArrayA6[0] + Environment.NewLine);

                // Change A6 to finished color
                inProgressEllipses[28].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA6[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A6];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A6],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A6 added to results grid

                // Add results grid data for A6 to output file
                outputFileData = positions[(int)steppingPositions.A6] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[28].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A6 End

                // A7 Start
                // ----------------------------------------------------------------------------------------------------------------------------

                // Move from A6 to A7
                moveX(xPos[(int)steppingPositions.A7] - xPos[(int)steppingPositions.A6]);
                moveY(yPos[(int)steppingPositions.A7] - yPos[(int)steppingPositions.A6]);

                // Change A7 to in progress color
                inProgressEllipses[29].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A7", "Reading", 1000);
                File.AppendAllText(logFilePath, "Reading A7" + Environment.NewLine);

                // Read A7
                StringBuilder sbA7 = new StringBuilder(100000);

                IntPtr testBoardValuePtrA7 = testGetBoardValue(sbA7, sbA7.Capacity);
                double[] testArrayA7 = new double[5];
                Marshal.Copy(testBoardValuePtrA7, testArrayA7, 0, 5);
                string inputStringA7 = "";
                inputStringA7 += "Return Value: m_dAvgValue = " + testArrayA7[0] + "\n";
                inputStringA7 += "Return Value: m_dCumSum = " + testArrayA7[1] + "\n";
                inputStringA7 += "Return Value: m_dLEDtmp = " + testArrayA7[2] + "\n";
                inputStringA7 += "Return Value: m_dPDtmp = " + testArrayA7[3] + "\n";
                inputStringA7 += "Return Value: testGetBoardValue = " + testArrayA7[4] + "\n";

                File.AppendAllText(logFilePath, "A7 avg value = " + testArrayA7[0] + Environment.NewLine);

                // Change A7 to finished color
                inProgressEllipses[29].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = (testArrayA7[0] - afeShiftFactor) * afeScaleFactor;
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                testResult = "NEG";

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A7];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.A7],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for A7 added to results grid

                // Add results grid data for A7 to output file
                outputFileData = positions[(int)steppingPositions.A7] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                resultsTextboxes[29].Text = raw_avg.ToString();

                // ----------------------------------------------------------------------------------------------------------------------------
                // A7 End... Reading Complete

                AutoClosingMessageBox.Show("All wells read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All wells read" + Environment.NewLine);

                testCloseTasksAndChannels();

                //MessageBox.Show("Moving back to load position");
                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.A7] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.A7] + yPos[(int)steppingPositions.Dispense_to_Read]));
                
                //MessageBox.Show("Reading complete, displaying results");
                AutoClosingMessageBox.Show("Reading complete, displaying results", "Results", 2000);
                File.AppendAllText(logFilePath, "Reading complete, displaying results" + Environment.NewLine);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_cart34.Visibility = Visibility.Hidden;
                inProgress_cart34_border.Visibility = Visibility.Hidden;

                File.AppendAllText(logFilePath, "In progress boxes hidden" + Environment.NewLine);

                results_grid.Visibility = Visibility.Visible;

                File.AppendAllText(logFilePath, "Results grid visible" + Environment.NewLine);

                if (switchResults_cb.IsChecked == true)
                {
                    results_grid.Visibility = Visibility.Hidden;
                    resultsDisplay_cart34_border.Visibility = Visibility.Visible;
                    resultsDisplay_cart34.Visibility = Visibility.Visible;
                }

                isReading = false;
                File.AppendAllText(logFilePath, "Reading completed" + Environment.NewLine);
            }

            else
            {
                MessageBox.Show("Please enter either 4 or 30 samples and click read button again.");
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

                IntPtr testBoardValuePtr = testGetBoardValue(sb2, sb2.Capacity);
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
