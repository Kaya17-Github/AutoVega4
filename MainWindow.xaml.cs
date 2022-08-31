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

                // Wait at least 30 mins, up to 1 hour
                AutoClosingMessageBox.Show("Incubating for 30 minutes", "Incubating", 3000);
                //Task.Delay(1800000).Wait();
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

                // Draw Probe for first six wells (A1, A2, B2, C2, D2, E2)
                AutoClosingMessageBox.Show("Drawing Probe", "Drawing Probe", 1000);
                File.AppendAllText(logFilePath, "Drawing Probe" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.Probe_Bottle]);

                // Draw 1386 steps (1.98mL)
                drawLiquid(1386);

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

                //**A2**//
                // Move from A1 to A2
                moveX(xPos[(int)steppingPositions.A2] - xPos[(int)steppingPositions.A1]);
                moveY(yPos[(int)steppingPositions.A2] - yPos[(int)steppingPositions.A1]);

                // Change A2 to in progress color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in A2", "Dispensing", 1000);

                // Dispense 330ul Probe in A2
                dispenseLiquid(231);

                // Change A2 to finished color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**B2**//
                // Move from A2 to B2
                moveX(xPos[(int)steppingPositions.B2] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.B2] - yPos[(int)steppingPositions.A2]);

                // Change B2 to in progress color
                inProgressB2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in B2", "Dispensing", 1000);

                // Dispense 330ul Probe in B2
                dispenseLiquid(231);

                // Change B2 to finished color
                inProgressB2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**C2**//
                // Move from B2 to C2
                moveX(xPos[(int)steppingPositions.C2] - xPos[(int)steppingPositions.B2]);
                moveY(yPos[(int)steppingPositions.C2] - yPos[(int)steppingPositions.B2]);

                // Change C2 to in progress color
                inProgressC2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in C2", "Dispensing", 1000);

                // Dispense 330ul Probe in C2
                dispenseLiquid(231);

                // Change C2 to finished color
                inProgressC2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**D2**//
                // Move from C2 to D2
                moveX(xPos[(int)steppingPositions.D2] - xPos[(int)steppingPositions.C2]);
                moveY(yPos[(int)steppingPositions.D2] - yPos[(int)steppingPositions.C2]);

                // Change D2 to in progress color
                inProgressD2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in D2", "Dispensing", 1000);

                // Dispense 330ul Probe in D2
                dispenseLiquid(231);

                // Change D2 to finished color
                inProgressD2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**E2**//
                // Move from D2 to E2
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.D2]);
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.D2]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing Probe in E2", "Dispensing", 1000);

                // Dispense 330ul Probe in E2
                dispenseLiquid(331);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

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

                // Move from E2 to Probe_Wash_Bottle
                moveX(xPos[(int)steppingPositions.Probe_Wash_Bottle] - xPos[(int)steppingPositions.E2]);
                moveY(yPos[(int)steppingPositions.Probe_Wash_Bottle] - yPos[(int)steppingPositions.E2]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.Probe_Wash_Bottle]);

                // Draw 1400 steps (2mL)
                drawLiquid(1400);

                // Dispense 1500 steps (2mL + extra)
                dispenseLiquid(1500);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.Probe_Wash_Bottle]);

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

                // Draw 2100 steps (3mL)
                drawLiquid(2100);

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

                // Dispense 500ul RB in A1
                dispenseLiquid(350);

                // Change A1 to finished color
                inProgressA1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**A2**//
                // Move from A1 to A2
                moveX(xPos[(int)steppingPositions.A2] - xPos[(int)steppingPositions.A1]);
                moveY(yPos[(int)steppingPositions.A2] - yPos[(int)steppingPositions.A1]);

                // Change A2 to in progress color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in A2", "Dispensing", 1000);

                // Dispense 500ul RB in A2
                dispenseLiquid(350);

                // Change A2 to finished color
                inProgressA2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**B2**//
                // Move from A2 to B2
                moveX(xPos[(int)steppingPositions.B2] - xPos[(int)steppingPositions.A2]);
                moveY(yPos[(int)steppingPositions.B2] - yPos[(int)steppingPositions.A2]);

                // Change B2 to in progress color
                inProgressB2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in B2", "Dispensing", 1000);

                // Dispense 500ul RB in B2
                dispenseLiquid(350);

                // Change B2 to finished color
                inProgressB2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**C2**//
                // Move from B2 to C2
                moveX(xPos[(int)steppingPositions.C2] - xPos[(int)steppingPositions.B2]);
                moveY(yPos[(int)steppingPositions.C2] - yPos[(int)steppingPositions.B2]);

                // Change C2 to in progress color
                inProgressC2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in C2", "Dispensing", 1000);

                // Dispense 500ul RB in C2
                dispenseLiquid(350);

                // Change C2 to finished color
                inProgressC2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**D2**//
                // Move from C2 to D2
                moveX(xPos[(int)steppingPositions.D2] - xPos[(int)steppingPositions.C2]);
                moveY(yPos[(int)steppingPositions.D2] - yPos[(int)steppingPositions.C2]);

                // Change D2 to in progress color
                inProgressD2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in D2", "Dispensing", 1000);

                // Dispense 500ul RB in D2
                dispenseLiquid(350);

                // Change D2 to finished color
                inProgressD2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //**E2**//
                // Move from D2 to E2
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.D2]);
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.D2]);

                // Change E2 to in progress color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Dispensing RB in E2", "Dispensing", 1000);

                // Dispense 500ul RB in E2
                dispenseLiquid(450);

                // Change E2 to finished color
                inProgressE2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

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

                // Move from E2 to RB_Wash_Bottle
                moveX(xPos[(int)steppingPositions.RB_Wash_Bottle] - xPos[(int)steppingPositions.E2]);
                moveY(yPos[(int)steppingPositions.RB_Wash_Bottle] - yPos[(int)steppingPositions.E2]);

                // Lower pipette tips
                lowerZPosition(zPos[(int)steppingPositions.RB_Wash_Bottle]);

                // Draw 2100 steps (3mL)
                drawLiquid(2100);

                // Dispense 2200 steps (3mL + extra)
                dispenseLiquid(2200);

                // Raise pipette tips
                raiseZPosition(zPos[(int)steppingPositions.RB_Wash_Bottle]);

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

                StringBuilder sb = new StringBuilder(50000);
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
                // ------------------------------------------------------------------------------------------------------------------------

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

                // Change A1 to finished color
                inProgressEllipses[0].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayA1[0];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.A1];

                testResults.Add(new TestResults()
                {
                    WellName = positions[10],
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

                // ------------------------------------------------------------------------------------------------------------------------
                // A1 End

                // A2 Start
                // ------------------------------------------------------------------------------------------------------------------------

                // Move from A1 to A2
                moveY(yPos[(int)steppingPositions.A2] - yPos[(int)steppingPositions.A1]);
                moveX(xPos[(int)steppingPositions.A2] - xPos[(int)steppingPositions.A1]);

                // Change A2 to in progress color
                inProgressEllipses[8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading A2", "Reading", 200);
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

                // Change A2 to finished color
                inProgressEllipses[8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayA2[0];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

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

                // ------------------------------------------------------------------------------------------------------------------------
                // A2 End

                // B2 Start
                // ------------------------------------------------------------------------------------------------------------------------

                // Move from A2 to B2
                moveY(yPos[(int)steppingPositions.B2] - yPos[(int)steppingPositions.A2]);
                moveX(xPos[(int)steppingPositions.B2] - xPos[(int)steppingPositions.A2]);

                // Change B2 to in progress color
                inProgressEllipses[7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading B2", "Reading", 200);
                File.AppendAllText(logFilePath, "Reading B2" + Environment.NewLine);

                // Read B2
                StringBuilder sbB2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrB2 = testGetBoardValue(sbB2, sbB2.Capacity);
                double[] testArrayB2 = new double[5];
                Marshal.Copy(testBoardValuePtrB2, testArrayB2, 0, 5);
                string inputStringB2 = "";
                inputStringB2 += "Return Value: m_dAvgValue = " + testArrayB2[0] + "\n";
                inputStringB2 += "Return Value: m_dCumSum = " + testArrayB2[1] + "\n";
                inputStringB2 += "Return Value: m_dLEDtmp = " + testArrayB2[2] + "\n";
                inputStringB2 += "Return Value: m_dPDtmp = " + testArrayB2[3] + "\n";
                inputStringB2 += "Return Value: testGetBoardValue = " + testArrayB2[4] + "\n";

                // Change B2 to finished color
                inProgressEllipses[7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayB2[0];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.B2];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.B2],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for B2 added to results grid

                // Add results grid data for B2 to output file
                outputFileData = positions[(int)steppingPositions.B2] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // ------------------------------------------------------------------------------------------------------------------------
                // B2 End

                // C2 Start
                // ------------------------------------------------------------------------------------------------------------------------

                // Move from B2 to C2
                moveY(yPos[(int)steppingPositions.C2] - yPos[(int)steppingPositions.B2]);
                moveX(xPos[(int)steppingPositions.C2] - xPos[(int)steppingPositions.B2]);

                // Change C2 to in progress color
                inProgressEllipses[6].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading C2", "Reading", 200);
                File.AppendAllText(logFilePath, "Reading C2" + Environment.NewLine);

                // Read C2
                StringBuilder sbC2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrC2 = testGetBoardValue(sbC2, sbC2.Capacity);
                double[] testArrayC2 = new double[5];
                Marshal.Copy(testBoardValuePtrC2, testArrayC2, 0, 5);
                string inputStringC2 = "";
                inputStringC2 += "Return Value: m_dAvgValue = " + testArrayC2[0] + "\n";
                inputStringC2 += "Return Value: m_dCumSum = " + testArrayC2[1] + "\n";
                inputStringC2 += "Return Value: m_dLEDtmp = " + testArrayC2[2] + "\n";
                inputStringC2 += "Return Value: m_dPDtmp = " + testArrayC2[3] + "\n";
                inputStringC2 += "Return Value: testGetBoardValue = " + testArrayC2[4] + "\n";

                // Change C2 to finished color
                inProgressEllipses[6].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayC2[0];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.C2];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.C2],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for C2 added to results grid

                // Add results grid data for C2 to output file
                outputFileData = positions[(int)steppingPositions.C2] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // ------------------------------------------------------------------------------------------------------------------------
                // C2 End

                // D2 Start
                // ------------------------------------------------------------------------------------------------------------------------

                // Move from C2 to D2
                moveY(yPos[(int)steppingPositions.D2] - yPos[(int)steppingPositions.C2]);
                moveX(xPos[(int)steppingPositions.D2] - xPos[(int)steppingPositions.C2]);

                // Change D2 to in progress color
                inProgressEllipses[5].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading D2", "Reading", 200);
                File.AppendAllText(logFilePath, "Reading D2" + Environment.NewLine);

                // Read D2
                StringBuilder sbD2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrD2 = testGetBoardValue(sbD2, sbD2.Capacity);
                double[] testArrayD2 = new double[5];
                Marshal.Copy(testBoardValuePtrD2, testArrayD2, 0, 5);
                string inputStringD2 = "";
                inputStringD2 += "Return Value: m_dAvgValue = " + testArrayD2[0] + "\n";
                inputStringD2 += "Return Value: m_dCumSum = " + testArrayD2[1] + "\n";
                inputStringD2 += "Return Value: m_dLEDtmp = " + testArrayD2[2] + "\n";
                inputStringD2 += "Return Value: m_dPDtmp = " + testArrayD2[3] + "\n";
                inputStringD2 += "Return Value: testGetBoardValue = " + testArrayD2[4] + "\n";

                // Change D2 to finished color
                inProgressEllipses[5].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayD2[0];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[(int)steppingPositions.D2];

                testResults.Add(new TestResults()
                {
                    WellName = positions[(int)steppingPositions.D2],
                    BackgroundReading = bgd_rdg.ToString(),
                    Threshold = threshold.ToString(),
                    RawAvg = raw_avg.ToString(),
                    TC_rdg = TC_rdg.ToString(),
                    TestResult = testResult,
                    SampleName = sampleName
                });

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for D2 added to results grid

                // Add results grid data for D2 to output file
                outputFileData = positions[(int)steppingPositions.D2] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // ------------------------------------------------------------------------------------------------------------------------
                // D2 End

                // E2 Start
                // ------------------------------------------------------------------------------------------------------------------------

                // Move from D2 to E2
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.D2]);
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.D2]);

                // Change E2 to in progress color
                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                AutoClosingMessageBox.Show("Reading E2", "Reading", 200);
                File.AppendAllText(logFilePath, "Reading E2" + Environment.NewLine);

                // Read E2
                StringBuilder sbE2 = new StringBuilder(100000);

                IntPtr testBoardValuePtrE2 = testGetBoardValue(sbE2, sbE2.Capacity);
                double[] testArrayE2 = new double[5];
                Marshal.Copy(testBoardValuePtrE2, testArrayE2, 0, 5);
                string inputStringE2 = "";
                inputStringE2 += "Return Value: m_dAvgValue = " + testArrayE2[0] + "\n";
                inputStringE2 += "Return Value: m_dCumSum = " + testArrayE2[1] + "\n";
                inputStringE2 += "Return Value: m_dLEDtmp = " + testArrayE2[2] + "\n";
                inputStringE2 += "Return Value: m_dPDtmp = " + testArrayE2[3] + "\n";
                inputStringE2 += "Return Value: testGetBoardValue = " + testArrayE2[4] + "\n";

                // Change E2 to finished color
                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayE2[0];
                raw_avg = Math.Round(raw_avg, 3);

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

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

                results_grid.ItemsSource = testResults;
                results_grid.Items.Refresh();
                // Result for E2 added to results grid

                // Add results grid data for E2 to output file
                outputFileData = positions[(int)steppingPositions.E2] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // ------------------------------------------------------------------------------------------------------------------------
                // E2 End

                AutoClosingMessageBox.Show("All cartridges read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All cartridges read" + Environment.NewLine);

                testCloseTasksAndChannels();

                //MessageBox.Show("Moving back to load position");
                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.E2] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.E2] + yPos[(int)steppingPositions.Dispense_to_Read]));
                
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
