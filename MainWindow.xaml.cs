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
        string logFilePath;
        string outputFilePath;
        string timeStamp;
        string testTime;
        string writeAllSteps;
        string readLimSwitches;
        string switchBanks;
        string delimiter;
        string[] map;
        string testResult;
        string sampleName;
        string outputFileData;
        int drainTime = 30000; //wait for 1 minute
        int wait = 0;
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

            logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega4_logfile.txt";
            outputFilePath = @"C:\Users\Public\Documents\Kaya17\Data\kaya17-AutoVega4_" + timeStamp + ".csv";

            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            timeStamp = DateTime.Now.ToString("ddMMMyy_HHmmss");
            testTime = DateTime.Now.ToString("ddMMM_HHmm");

            writeAllSteps = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[3];
            readLimSwitches = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[4];
            switchBanks = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[5];

            map = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\34_well_cartridge_steps.csv");

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
            WB_Bottle_1 = 5,
            WB_Bottle_2 = 6,
            RB_Bottle_1 = 7,
            RB_Bottle_2 = 8,
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
            if (sampleNum_tb.Text == "4")
            {
                string inProgressColor = "#F5D5CB";
                string finishedColor = "#D7ECD9";

                // define test port for turning on and off vacuum pump
                string test3 = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[2];
                isReading = true;
                File.AppendAllText(logFilePath, "Reading started" + Environment.NewLine);
                List<TestResults> testResults = new List<TestResults>();

                // define array for necessary items from parameter file
                double[] testArray = new double[17];

                // Show In Progress Boxes
                inProgressBG_r.Visibility = Visibility.Visible;
                inProgress_stack.Visibility = Visibility.Visible;
                inProgress_carts.Visibility = Visibility.Visible;
                inProgress_carts_borders.Visibility = Visibility.Visible;

                File.AppendAllText(logFilePath, "In progress boxes visible" + Environment.NewLine);

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

                MessageBox.Show(inputString);
                File.AppendAllText(logFilePath, inputString + Environment.NewLine);

                IntPtr testPtr = verifyInput(testArray);
                double[] testArray2 = new double[17];
                Marshal.Copy(testPtr, testArray2, 0, 17);
                inputString = "";
                foreach (double value in testArray)
                {
                    inputString += "Verify input: " + value + "\n";
                }

                MessageBox.Show(inputString);
                File.AppendAllText(logFilePath, inputString + Environment.NewLine);

                bool settingsBool = testSetSettings(testArray);

                MessageBox.Show("Test setSettings: " + settingsBool);
                File.AppendAllText(logFilePath, "Test setSettings: " + settingsBool + Environment.NewLine);

                string[] map = File.ReadAllLines("../../Auto Vega/single_well_cartridge_map.csv");
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

                int drainZ = zPos[15];
                int drawZ = zPos[16];
                int drawPump = pump[16];
                int dispensePump = pump[17];

                //MessageBox.Show("Moving to Drain Position");
                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 3000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveX(xPos[7] - xPos[6]);
                moveY(yPos[7] - yPos[6]);

                // Change Sample Draining Box and Cartridges to in progress color
                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Foreground = Brushes.Black;
                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Lower Pipette Tips to Drain
                lowerZPosition(drainZ);

                //MessageBox.Show("Wait 3 minutes for samples to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 3 minutes for samples to drain through cartridges", "Draining", 3000);
                File.AppendAllText(logFilePath, "Wait 3 minutes for samples to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test3, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on for 4 minutes
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test3, "port2",
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

                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("Sample Draining Complete");
                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 3000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(drainZ);

                // Move to WB Bottle
                moveX(xPos[0] - xPos[7]);
                moveY(yPos[0] - yPos[7]);

                cart1.Fill = Brushes.Gray;
                cart2.Fill = Brushes.Gray;
                cart3.Fill = Brushes.Gray;
                cart4.Fill = Brushes.Gray;

                //MessageBox.Show("Drawing liquid from " + positions[0]);
                AutoClosingMessageBox.Show("Drawing liquid from wash buffer bottle", "Drawing WB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from wash buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 1400 steps
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Foreground = Brushes.Black;

                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing WB in " + positions[2]);
                AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[2], "Dispensing WB", 3000);
                File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[2] + Environment.NewLine);

                // Move to Cartridge 1
                moveX(xPos[2] - xPos[0]);
                moveY(yPos[2] - yPos[0]);

                // Dispense 350 steps
                dispenseLiquid(dispensePump);

                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing WB in " + positions[3]);
                AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[3], "Dispensing WB", 3000);
                File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[3] + Environment.NewLine);

                // Move to Cartridge 2
                moveY(yPos[3] - yPos[2]);

                // Dispense 350 steps
                dispenseLiquid(dispensePump);

                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing WB in " + positions[4]);
                AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[4], "Dispensing WB", 3000);
                File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[4] + Environment.NewLine);

                // Move to Cartridge 3
                moveY(yPos[4] - yPos[3]);

                // Dispense 350 steps
                dispenseLiquid(dispensePump);

                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing WB in " + positions[5]);
                AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[5], "Dispensing WB", 3000);
                File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[5] + Environment.NewLine);

                // Move to Cartridge 4
                moveY(yPos[5] - yPos[4]);

                // Dispense 450 steps
                dispenseLiquid(pump[18]);

                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("WB Dispensed");
                AutoClosingMessageBox.Show("Wash Buffer Dispensed", "Dispensing Complete", 3000);
                File.AppendAllText(logFilePath, "Wash Buffer Dispensed" + Environment.NewLine);

                cart1.Fill = Brushes.Gray;
                cart2.Fill = Brushes.Gray;
                cart3.Fill = Brushes.Gray;
                cart4.Fill = Brushes.Gray;

                //MessageBox.Show("Moving Back to Drain Position");
                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 3000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveX(xPos[7] - xPos[5]);
                moveY(yPos[7] - yPos[5]);

                wbDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_tb.Foreground = Brushes.Black;
                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Lower Pipette Tips to Drain
                lowerZPosition(drainZ);

                //MessageBox.Show("Wait 4 minutes for WB to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 4 minutes for WB to drain through cartridges", "Draining", 3000);
                File.AppendAllText(logFilePath, "Wait 4 minutes for WB to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test3, "port2",
                            ChannelLineGrouping.OneChannelForAllLines);

                        //  Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                        //  of digital data on demand, so no timeout is necessary.
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, 1);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Leave pump on for 4 minutes
                Task.Delay(drainTime).Wait();

                // Turn pump off
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test3, "port2",
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

                wbDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("WB Draining Complete");
                AutoClosingMessageBox.Show("Wash buffer draining complete", "Draining Complete", 3000);
                File.AppendAllText(logFilePath, "Wash buffer draining complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(drainZ);

                // Move to RB Bottle
                moveX(xPos[1] - xPos[7]);
                moveY(yPos[1] - yPos[7]);

                cart1.Fill = Brushes.Gray;
                cart2.Fill = Brushes.Gray;
                cart3.Fill = Brushes.Gray;
                cart4.Fill = Brushes.Gray;

                //MessageBox.Show("Drawing liquid from " + positions[1]);
                AutoClosingMessageBox.Show("Drawing liquid from read buffer bottle", "Drawing RB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from read buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 1400 steps
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing RB in " + positions[2]);
                AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[2], "Dispensing RB", 3000);
                File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[2] + Environment.NewLine);

                // Move to Cartridge 1
                moveX(xPos[2] - xPos[1]);
                moveY(yPos[2] - yPos[1]);

                // Dispense 350 steps
                dispenseLiquid(dispensePump);

                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing RB in " + positions[3]);
                AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[3], "Dispensing RB", 3000);
                File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[3] + Environment.NewLine);

                // Move to Cartridge 2
                moveY(yPos[3] - yPos[2]);

                // Dispense 350 steps
                dispenseLiquid(dispensePump);

                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing RB in " + positions[4]);
                AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[4], "Dispensing RB", 3000);
                File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[4] + Environment.NewLine);

                // Move to Cartridge 3
                moveY(yPos[4] - yPos[3]);

                // Dispense 350 steps
                dispenseLiquid(dispensePump);

                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Dispensing RB in " + positions[5]);
                AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[5], "Dispensing RB", 3000);
                File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[5] + Environment.NewLine);

                // Move to Cartridge 4
                moveY(yPos[5] - yPos[4]);

                // Dispense 450 steps
                dispenseLiquid(pump[18]);

                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("RB Dispensed");
                AutoClosingMessageBox.Show("Read buffer dispensed", "Dispensing Complete", 3000);
                File.AppendAllText(logFilePath, "Read buffer dispensed" + Environment.NewLine);

                cart1.Fill = Brushes.Gray;
                cart2.Fill = Brushes.Gray;
                cart3.Fill = Brushes.Gray;
                cart4.Fill = Brushes.Gray;

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                reading_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_tb.Foreground = Brushes.Black;

                //MessageBox.Show("Reading samples in all wells");
                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // Move to R1 (Reading A1)
                moveX(xPos[8] - xPos[5]);
                moveY(yPos[8] - yPos[5]);

                // TODO: Add Board initialization function here
                StringBuilder sb = new StringBuilder(5000);
                bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

                MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());
                File.AppendAllText(logFilePath, "Test initializeBoard: " + initializeBoardBool + Environment.NewLine);

                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                //MessageBox.Show("Reading A1");
                AutoClosingMessageBox.Show("Reading A1", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading A1" + Environment.NewLine);

                // Reading code here
                //Task.Delay(3000).Wait();
                StringBuilder sbA1 = new StringBuilder(10000);

                IntPtr testBoardValuePtrA1 = testGetBoardValue(sbA1, sbA1.Capacity);
                double[] testArrayA1 = new double[5];
                Marshal.Copy(testBoardValuePtrA1, testArrayA1, 0, 5);
                string inputStringA1 = "";
                inputStringA1 += "Return Value: m_dAvgValue = " + testArrayA1[0] + "\n";
                //avgValues.Append(testArrayA1[0]);
                //avgValues[1] = testArrayA1[0];
                inputStringA1 += "Return Value: m_dCumSum = " + testArrayA1[1] + "\n";
                inputStringA1 += "Return Value: m_dLEDtmp = " + testArrayA1[2] + "\n";
                inputStringA1 += "Return Value: m_dPDtmp = " + testArrayA1[3] + "\n";
                inputStringA1 += "Return Value: testGetBoardValue = " + testArrayA1[4] + "\n";

                MessageBox.Show(inputStringA1 + sbA1.ToString());

                cart1.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Move to R2 (Reading A2)
                moveY(yPos[9] - yPos[8]);

                //MessageBox.Show("Reading A2");
                AutoClosingMessageBox.Show("Reading A2", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading A2" + Environment.NewLine);

                // Reading code here
                //Task.Delay(3000).Wait();
                StringBuilder sbA2 = new StringBuilder(10000);

                IntPtr testBoardValuePtrA2 = testGetBoardValue(sbA2, sbA2.Capacity);
                double[] testArrayA2 = new double[5];
                Marshal.Copy(testBoardValuePtrA2, testArrayA2, 0, 5);
                string inputStringA2 = "";
                inputStringA2 += "Return Value: m_dAvgValue = " + testArrayA2[0] + "\n";
                //avgValues.Append(testArrayA2[0]);
                //avgValues[2] = testArrayA2[0];
                inputStringA2 += "Return Value: m_dCumSum = " + testArrayA2[1] + "\n";
                inputStringA2 += "Return Value: m_dLEDtmp = " + testArrayA2[2] + "\n";
                inputStringA2 += "Return Value: m_dPDtmp = " + testArrayA2[3] + "\n";
                inputStringA2 += "Return Value: testGetBoardValue = " + testArrayA2[4] + "\n";

                MessageBox.Show(inputStringA2 + sbA2.ToString());

                cart2.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Move to R3 (Reading A3)
                moveY(yPos[10] - yPos[9]);

                //MessageBox.Show("Reading A3");
                AutoClosingMessageBox.Show("Reading A3", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading A3" + Environment.NewLine);

                // Reading code here
                //Task.Delay(3000).Wait();
                StringBuilder sbA3 = new StringBuilder(10000);

                IntPtr testBoardValuePtrA3 = testGetBoardValue(sbA3, sbA3.Capacity);
                double[] testArrayA3 = new double[5];
                Marshal.Copy(testBoardValuePtrA3, testArrayA3, 0, 5);
                string inputStringA3 = "";
                inputStringA3 += "Return Value: m_dAvgValue = " + testArrayA3[0] + "\n";
                //avgValues.Append(testArrayA3[0]);
                //avgValues[3] = testArrayA3[0];
                inputStringA3 += "Return Value: m_dCumSum = " + testArrayA3[1] + "\n";
                inputStringA3 += "Return Value: m_dLEDtmp = " + testArrayA3[2] + "\n";
                inputStringA3 += "Return Value: m_dPDtmp = " + testArrayA3[3] + "\n";
                inputStringA3 += "Return Value: testGetBoardValue = " + testArrayA3[4] + "\n";

                MessageBox.Show(inputStringA3 + sbA3.ToString());

                cart3.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Move to R4 (Reading A4)
                moveY(yPos[11] - yPos[10]);

                //MessageBox.Show("Reading A4");
                AutoClosingMessageBox.Show("Reading A4", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading A4" + Environment.NewLine);

                // Reading code here
                //Task.Delay(3000).Wait();
                StringBuilder sbA4 = new StringBuilder(10000);

                IntPtr testBoardValuePtrA4 = testGetBoardValue(sbA4, sbA4.Capacity);
                double[] testArrayA4 = new double[5];
                Marshal.Copy(testBoardValuePtrA4, testArrayA4, 0, 5);
                string inputStringA4 = "";
                inputStringA4 += "Return Value: m_dAvgValue = " + testArrayA4[0] + "\n";
                //avgValues.Append(testArrayA4[0]);
                //avgValues[4] = testArrayA4[0];
                inputStringA4 += "Return Value: m_dCumSum = " + testArrayA4[1] + "\n";
                inputStringA4 += "Return Value: m_dLEDtmp = " + testArrayA4[2] + "\n";
                inputStringA4 += "Return Value: m_dPDtmp = " + testArrayA4[3] + "\n";
                inputStringA4 += "Return Value: testGetBoardValue = " + testArrayA4[4] + "\n";

                MessageBox.Show(inputStringA4 + sbA4.ToString());

                cart4.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("All cartridges read");
                AutoClosingMessageBox.Show("All cartridges read", "Reading Complete", 3000);
                File.AppendAllText(logFilePath, "All cartridges read" + Environment.NewLine);

                // TODO: add close tasks and channels function here
                testCloseTasksAndChannels();

                //MessageBox.Show("Moving back to load position");
                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 3000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[6] - xPos[11]);
                moveY(yPos[6] - yPos[11]);

                //MessageBox.Show("Reading complete, displaying results");
                AutoClosingMessageBox.Show("Reading complete, displaying results", "Results", 3000);
                File.AppendAllText(logFilePath, "Reading complete, displaying results" + Environment.NewLine);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_carts.Visibility = Visibility.Hidden;
                inProgress_carts_borders.Visibility = Visibility.Hidden;

                File.AppendAllText(logFilePath, "In progress boxes hidden" + Environment.NewLine);

                results_grid.Visibility = Visibility.Visible;

                File.AppendAllText(logFilePath, "Results grid visible" + Environment.NewLine);

                // Display Results

                for (int i = 2; i < 6; i++)
                {
                    double[] avgValues = { testArrayA1[0], testArrayA2[0], testArrayA3[0], testArrayA4[0] };

                    // threshold and bgd_rdg from parameter file
                    double averageSignal = avgValues[i - 2];

                    // calculate raw_avg = (averageSignal - afeShiftFactor)*afeScaleFactor
                    double raw_avg = (averageSignal - afeShiftFactor) * afeScaleFactor;

                    // calculate TC_rdg = raw_avg
                    double TC_rdg = raw_avg;

                    // calculate diff = (TC_rdg - viralCountOffset)*viralScaleFactor
                    double diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                    // testResult = NEG by default
                    string testResult = "NEG";

                    // testResult = POS if diff > threshold
                    if (diff > threshold)
                    {
                        testResult = "POS";
                    }

                    string sampleName = positions[i];

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

                    results_grid.ItemsSource = testResults;
                    results_grid.Items.Refresh();
                }

                isReading = false;
                File.AppendAllText(logFilePath, "Reading completed" + Environment.NewLine);
            }

            else if (sampleNum_tb.Text == "30")
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

                //MessageBox.Show("Moving to Drain Position");
                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 2000);
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

                //MessageBox.Show("Wait 1 minute for samples to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 1 minute for samples to drain through cartridges", "Draining", 2000);
                File.AppendAllText(logFilePath, "Wait 1 minute for samples to drain through cartridges" + Environment.NewLine);

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
                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 2000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move to WB Bottle 1
                moveX(xPos[(int)steppingPositions.WB_Bottle_1] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.WB_Bottle_1] - yPos[(int)steppingPositions.Drain]);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Draw WB for first row
                AutoClosingMessageBox.Show("Drawing WB for first row", "Drawing WB", 2000);
                File.AppendAllText(logFilePath, "Drawing WB for first row" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.WB_Bottle_1]);

                // Draw 1120 steps (1.6mL)
                drawLiquid(1120);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.WB_Bottle_1]);

                // Change WB Dispense Box to in progress color
                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Foreground = Brushes.Black;

                // Dispense 400ul WB in first row (A1, B1, C1, D1)
                for (int i = 10; i < 14; i++)
                {
                    if (i == 10)
                    {
                        // Change A1 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change A1, B1, and C1 to in progress color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change B1, C1, and D1 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 10)
                    {
                        // Move from WB Bottle 1 to A1
                        moveY(yPos[i] - yPos[(int)steppingPositions.WB_Bottle_1]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.WB_Bottle_1]);
                    }
                    else
                    {
                        // Move from A1 to B1, B1 to C1, and C1 to D1
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change D1 to finished color
                inProgressEllipses[3].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Move back to WB Bottle 1
                moveX(xPos[(int)steppingPositions.WB_Bottle_1] - xPos[(int)steppingPositions.D1]);
                moveY(yPos[(int)steppingPositions.WB_Bottle_1] - yPos[(int)steppingPositions.D1]);

                // Draw WB for Rows 2 and 3
                AutoClosingMessageBox.Show("Drawing WB for rows 2 and 3", "Drawing WB", 2000);
                File.AppendAllText(logFilePath, "Drawing WB for rows 2 and 3" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.WB_Bottle_1]);

                // Draw 2800 steps (4mL)
                drawLiquid(pump[(int)steppingPositions.WB_Bottle_1]);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.WB_Bottle_1]);

                // Dispense 400ul WB in Rows 2 and 3 (E2, D2, C2, B2, A2, A3, B3, C3, D3, E3)
                for (int i = 14; i < 24; i++)
                {
                    if (i == 14)
                    {
                        // Change E2 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change E2, D2, C2, B2, A2, A3, B3, C3, and D3 to finished color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change D2, C2, B2, A2, A3, B3, C3, D3, and E3 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 14)
                    {
                        // Move from WB Bottle 1 to E2
                        moveY(yPos[i] - yPos[(int)steppingPositions.WB_Bottle_1]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.WB_Bottle_1]);
                    }
                    else
                    {
                        // Move from E2 to D2, etc.
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change E3 to finished color
                inProgressEllipses[13].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Move to WB Bottle 2
                moveX(xPos[(int)steppingPositions.WB_Bottle_2] - xPos[(int)steppingPositions.E3]);
                moveY(yPos[(int)steppingPositions.WB_Bottle_2] - yPos[(int)steppingPositions.E3]);

                // Draw WB for Rows 4 and 5
                AutoClosingMessageBox.Show("Drawing WB for rows 4 and 5", "Drawing WB", 2000);
                File.AppendAllText(logFilePath, "Drawing WB for rows 4 and 5" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.WB_Bottle_2]);

                // Draw 2800 steps (4mL)
                drawLiquid(pump[(int)steppingPositions.WB_Bottle_2]);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.WB_Bottle_2]);

                // Dispense 400ul WB in Rows 4 and 5 (E4, D4, C4, B4, A4, A5, B5, C5, D5, E5)
                for (int i = 24; i < 34; i++)
                {
                    if (i == 24)
                    {
                        // Change E4 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change E4, D4, C4, B4, A4, A5, B5, C5, and D5 to finished color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change D4, C4, B4, A4, A5, B5, C5, D5, and E5 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 24)
                    {
                        // Move from WB Bottle 2 to E4
                        moveY(yPos[i] - yPos[(int)steppingPositions.WB_Bottle_2]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.WB_Bottle_2]);
                    }
                    else
                    {
                        // Move from E4 to D4, etc.
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change E5 to finished color
                inProgressEllipses[23].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Move back to WB Bottle 2
                moveX(xPos[(int)steppingPositions.WB_Bottle_2] - xPos[(int)steppingPositions.E5]);
                moveY(yPos[(int)steppingPositions.WB_Bottle_2] - yPos[(int)steppingPositions.E5]);

                // Draw WB for Rows 6 and 7
                AutoClosingMessageBox.Show("Drawing WB for rows 6 and 7", "Drawing WB", 2000);
                File.AppendAllText(logFilePath, "Drawing WB for rows 6 and 7" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.WB_Bottle_2]);

                // Draw 2800 steps (4mL)
                drawLiquid(pump[(int)steppingPositions.WB_Bottle_2]);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.WB_Bottle_2]);

                // Dispense 400ul WB in Rows 6 and 7 (E6, D6, C6, B6, A6, A7, B7, C7, D7, E7)
                for (int i = 34; i < 44; i++)
                {
                    if (i == 34)
                    {
                        // Change E6 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change E6, D6, C6, B6, A6, A7, B7, C7, and D7 to finished color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change D6, C6, B6, A6, A7, B7, C7, D7, and E7 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 34)
                    {
                        // Move from WB Bottle 2 to E6
                        moveY(yPos[i] - yPos[(int)steppingPositions.WB_Bottle_2]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.WB_Bottle_2]);
                    }
                    else
                    {
                        // Move from E6 to D6, etc.
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change E7 to finished color
                inProgressEllipses[33].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change WB Dispense box to finished color
                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("WB Dispensed");
                AutoClosingMessageBox.Show("Wash Buffer Dispensed", "Dispensing Complete", 2000);
                File.AppendAllText(logFilePath, "Wash Buffer Dispensed" + Environment.NewLine);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                //MessageBox.Show("Moving Back to Drain Position");
                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveY(yPos[(int)steppingPositions.Drain] - yPos[(int)steppingPositions.E7]);
                moveX(xPos[(int)steppingPositions.Drain] - xPos[(int)steppingPositions.E7]);

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

                //MessageBox.Show("Wait 5 minutes for WB to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 5 minutes for WB to drain through cartridges", "Draining", 3000);
                File.AppendAllText(logFilePath, "Wait 5 minutes for WB to drain through cartridges" + Environment.NewLine);

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

                // Leave pump on for 5 minutes
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

                //MessageBox.Show("WB Draining Complete");
                AutoClosingMessageBox.Show("Wash buffer draining complete", "Draining Complete", 3000);
                File.AppendAllText(logFilePath, "Wash buffer draining complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(zPos[(int)steppingPositions.Drain]);

                // Move to RB Bottle 1
                moveX(xPos[(int)steppingPositions.RB_Bottle_1] - xPos[(int)steppingPositions.Drain]);
                moveY(yPos[(int)steppingPositions.RB_Bottle_1] - yPos[(int)steppingPositions.Drain]);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Draw RB for first row
                AutoClosingMessageBox.Show("Drawing RB for first row", "Drawing WB", 2000);
                File.AppendAllText(logFilePath, "Drawing RB for first row" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle_1]);

                // Draw 1120 steps (1.6mL)
                drawLiquid(1120);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle_1]);

                // Change RB Dispense Box to in progress color
                rbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                rbDispense_tb.Foreground = Brushes.Black;

                // Dispense 400ul RB in first row (A1, B1, C1, D1)
                for (int i = 10; i < 14; i++)
                {
                    if (i == 10)
                    {
                        // Change A1 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change A1, B1, and C1 to in progress color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change B1, C1, and D1 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 10)
                    {
                        // Move from RB Bottle 1 to A1
                        moveY(yPos[i] - yPos[(int)steppingPositions.RB_Bottle_1]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.RB_Bottle_1]);
                    }
                    else
                    {
                        // Move from A1 to B1, B1 to C1, and C1 to D1
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change D1 to finished color
                inProgressEllipses[3].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Move back to RB Bottle 1
                moveX(xPos[(int)steppingPositions.RB_Bottle_1] - xPos[(int)steppingPositions.D1]);
                moveY(yPos[(int)steppingPositions.RB_Bottle_1] - yPos[(int)steppingPositions.D1]);

                // Draw RB for Rows 2 and 3
                AutoClosingMessageBox.Show("Drawing RB for rows 2 and 3", "Drawing RB", 2000);
                File.AppendAllText(logFilePath, "Drawing RB for rows 2 and 3" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle_1]);

                // Draw 2800 steps (4mL)
                drawLiquid(pump[(int)steppingPositions.RB_Bottle_1]);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle_1]);

                // Dispense 400ul RB in Rows 2 and 3 (E2, D2, C2, B2, A2, A3, B3, C3, D3, E3)
                for (int i = 14; i < 24; i++)
                {
                    if (i == 14)
                    {
                        // Change E2 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change E2, D2, C2, B2, A2, A3, B3, C3, and D3 to finished color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change D2, C2, B2, A2, A3, B3, C3, D3, and E3 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 14)
                    {
                        // Move from RB Bottle 1 to E2
                        moveY(yPos[i] - yPos[(int)steppingPositions.RB_Bottle_1]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.RB_Bottle_1]);
                    }
                    else
                    {
                        // Move from E2 to D2, etc.
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change E3 to finished color
                inProgressEllipses[13].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Move to RB Bottle 2
                moveX(xPos[(int)steppingPositions.RB_Bottle_2] - xPos[(int)steppingPositions.E3]);
                moveY(yPos[(int)steppingPositions.RB_Bottle_2] - yPos[(int)steppingPositions.E3]);

                // Draw RB for Rows 4 and 5
                AutoClosingMessageBox.Show("Drawing RB for rows 4 and 5", "Drawing RB", 2000);
                File.AppendAllText(logFilePath, "Drawing RB for rows 4 and 5" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle_2]);

                // Draw 2800 steps (4mL)
                drawLiquid(pump[(int)steppingPositions.RB_Bottle_2]);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle_2]);

                // Dispense 400ul RB in Rows 4 and 5 (E4, D4, C4, B4, A4, A5, B5, C5, D5, E5)
                for (int i = 24; i < 34; i++)
                {
                    if (i == 24)
                    {
                        // Change E4 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change E4, D4, C4, B4, A4, A5, B5, C5, and D5 to finished color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change D4, C4, B4, A4, A5, B5, C5, D5, and E5 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 24)
                    {
                        // Move from RB Bottle 2 to E4
                        moveY(yPos[i] - yPos[(int)steppingPositions.RB_Bottle_2]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.RB_Bottle_2]);
                    }
                    else
                    {
                        // Move from E4 to D4, etc.
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change E5 to finished color
                inProgressEllipses[23].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Move back to RB Bottle 2
                moveX(xPos[(int)steppingPositions.RB_Bottle_2] - xPos[(int)steppingPositions.E5]);
                moveY(yPos[(int)steppingPositions.RB_Bottle_2] - yPos[(int)steppingPositions.E5]);

                // Draw RB for Rows 6 and 7
                AutoClosingMessageBox.Show("Drawing RB for rows 6 and 7", "Drawing RB", 2000);
                File.AppendAllText(logFilePath, "Drawing RB for rows 6 and 7" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(zPos[(int)steppingPositions.RB_Bottle_2]);

                // Draw 2800 steps (4mL)
                drawLiquid(pump[(int)steppingPositions.RB_Bottle_2]);

                // Raise Z by 9000 steps
                raiseZPosition(zPos[(int)steppingPositions.RB_Bottle_2]);

                // Dispense 400ul RB in Rows 6 and 7 (E6, D6, C6, B6, A6, A7, B7, C7, D7, E7)
                for (int i = 34; i < 44; i++)
                {
                    if (i == 34)
                    {
                        // Change E6 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        // Change E6, D6, C6, B6, A6, A7, B7, C7, and D7 to finished color
                        inProgressEllipses[i - 11].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        // Change D6, C6, B6, A6, A7, B7, C7, D7, and E7 to in progress color
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 2000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 34)
                    {
                        // Move from RB Bottle 2 to E6
                        moveY(yPos[i] - yPos[(int)steppingPositions.RB_Bottle_2]);
                        moveX(xPos[i] - xPos[(int)steppingPositions.RB_Bottle_2]);
                    }
                    else
                    {
                        // Move from E6 to D6, etc.
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Change E7 to finished color
                inProgressEllipses[33].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Change RB Dispense box to finished color
                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("RB Dispensed");
                AutoClosingMessageBox.Show("Read Buffer Dispensed", "Dispensing Complete", 2000);
                MessageBox.Show("RB Dispensed");
                File.AppendAllText(logFilePath, "Read Buffer Dispensed" + Environment.NewLine);

                // Change Cartridges to gray
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

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

                inProgressEllipses[0].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Move from E7 to A1 + Dispense_to_Read
                moveY((yPos[(int)steppingPositions.A1] + yPos[(int)steppingPositions.Dispense_to_Read]) - yPos[(int)steppingPositions.E7]);
                moveX((xPos[(int)steppingPositions.A1] + xPos[(int)steppingPositions.Dispense_to_Read]) - xPos[(int)steppingPositions.E7]);

                AutoClosingMessageBox.Show("Reading A1", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading A1" + Environment.NewLine);

                StringBuilder sbA1 = new StringBuilder(10000);                

                IntPtr testBoardValuePtrA1 = testGetBoardValue(sbA1, sbA1.Capacity);
                double[] testArrayA1 = new double[5];
                Marshal.Copy(testBoardValuePtrA1, testArrayA1, 0, 5);
                string inputStringA1 = "";
                inputStringA1 += "Return Value: m_dAvgValue = " + testArrayA1[0] + "\n";
                inputStringA1 += "Return Value: m_dCumSum = " + testArrayA1[1] + "\n";
                inputStringA1 += "Return Value: m_dLEDtmp = " + testArrayA1[2] + "\n";
                inputStringA1 += "Return Value: m_dPDtmp = " + testArrayA1[3] + "\n";
                inputStringA1 += "Return Value: testGetBoardValue = " + testArrayA1[4] + "\n";

                inProgressEllipses[0].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                inProgressEllipses[1].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Update Results Grid
                raw_avg = testArrayA1[0];

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[10];

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
                outputFileData = positions[10] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // Move from A1 to B1
                moveY(yPos[(int)steppingPositions.B1] - yPos[(int)steppingPositions.A1]);
                moveX(xPos[(int)steppingPositions.B1] - xPos[(int)steppingPositions.A1]);

                AutoClosingMessageBox.Show("Reading B1", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading B1" + Environment.NewLine);

                StringBuilder sbB1 = new StringBuilder(10000);

                IntPtr testBoardValuePtrB1 = testGetBoardValue(sbB1, sbB1.Capacity);
                double[] testArrayB1 = new double[5];
                Marshal.Copy(testBoardValuePtrB1, testArrayB1, 0, 5);
                string inputStringB1 = "";
                inputStringB1 += "Return Value: m_dAvgValue = " + testArrayB1[0] + "\n";
                inputStringB1 += "Return Value: m_dCumSum = " + testArrayB1[1] + "\n";
                inputStringB1 += "Return Value: m_dLEDtmp = " + testArrayB1[2] + "\n";
                inputStringB1 += "Return Value: m_dPDtmp = " + testArrayB1[3] + "\n";
                inputStringB1 += "Return Value: testGetBoardValue = " + testArrayB1[4] + "\n";

                inProgressEllipses[1].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                // Update Results Grid
                raw_avg = testArrayB1[0];

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[11];

                testResults.Add(new TestResults()
                {
                    WellName = positions[11],
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
                outputFileData = positions[11] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // Move from B1 to E2
                moveX(xPos[(int)steppingPositions.E2] - xPos[(int)steppingPositions.B1]);
                moveY(yPos[(int)steppingPositions.E2] - yPos[(int)steppingPositions.B1]);

                AutoClosingMessageBox.Show("Reading E2", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading E2" + Environment.NewLine);

                StringBuilder sbE2 = new StringBuilder(10000);

                IntPtr testBoardValuePtrE2 = testGetBoardValue(sbE2, sbE2.Capacity);
                double[] testArrayE2 = new double[5];
                Marshal.Copy(testBoardValuePtrE2, testArrayE2, 0, 5);
                string inputStringE2 = "";
                inputStringE2 += "Return Value: m_dAvgValue = " + testArrayE2[0] + "\n";
                inputStringE2 += "Return Value: m_dCumSum = " + testArrayE2[1] + "\n";
                inputStringE2 += "Return Value: m_dLEDtmp = " + testArrayE2[2] + "\n";
                inputStringE2 += "Return Value: m_dPDtmp = " + testArrayE2[3] + "\n";
                inputStringE2 += "Return Value: testGetBoardValue = " + testArrayE2[4] + "\n";

                inProgressEllipses[4].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                // Update Results Grid
                raw_avg = testArrayE2[0];

                TC_rdg = raw_avg;

                diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

                if (diff > threshold)
                {
                    testResult = "POS";
                }

                sampleName = positions[14];

                testResults.Add(new TestResults()
                {
                    WellName = positions[14],
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
                outputFileData = positions[14] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                File.AppendAllText(outputFilePath, outputFileData);

                // Loop through rest of reading steps
                for (int i = 15; i < positions.Length; i++)
                {
                    // Move to next well
                    moveY(yPos[i] - yPos[i - 1]);
                    moveX(xPos[i] - xPos[i - 1]);

                    // Change current well to in progress color
                    inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);

                    // Read sample in current well
                    StringBuilder sbR = new StringBuilder(10000);

                    IntPtr boardValuePtr = testGetBoardValue(sbR, sbR.Capacity);
                    double[] readingValues = new double[5];
                    Marshal.Copy(boardValuePtr, readingValues, 0, 5);
                    string readingInputString = "";
                    readingInputString += "Return Value: m_dAvgValue = " + readingValues[0] + "\n";
                    readingInputString += "Return Value: m_dCumSum = " + readingValues[1] + "\n";
                    readingInputString += "Return Value: m_dLEDtmp = " + readingValues[2] + "\n";
                    readingInputString += "Return Value: m_dPDtmp = " + readingValues[3] + "\n";
                    readingInputString += "Return Value: testGetBoardValue = " + readingValues[4] + "\n";

                    // Update Results Grid for current well
                    raw_avg = readingInputString[0];

                    TC_rdg = raw_avg;

                    diff = (TC_rdg - viralCountOffsetFactor) * viralCountScaleFactor;

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

                    results_grid.ItemsSource = testResults;
                    results_grid.Items.Refresh();

                    // Add results grid data for current well to output file
                    outputFileData = positions[i] + delimiter + bgd_rdg.ToString() + delimiter + threshold.ToString() +
                                            delimiter + raw_avg.ToString() + delimiter + TC_rdg.ToString() + delimiter + testResult + Environment.NewLine;

                    File.AppendAllText(outputFilePath, outputFileData);

                    // Change current well to finished color and next well to in progress color except for last time
                    if (i == 43)
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 10].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 9].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }                    
                }

                AutoClosingMessageBox.Show("All cartridges read", "Reading Complete", 2000);
                File.AppendAllText(logFilePath, "All cartridges read" + Environment.NewLine);

                testCloseTasksAndChannels();

                //MessageBox.Show("Moving back to load position");
                AutoClosingMessageBox.Show("Moving back to load position", "Moving", 2000);
                File.AppendAllText(logFilePath, "Moving back to load position" + Environment.NewLine);

                // Move back to Load Position
                moveX(xPos[(int)steppingPositions.Load] - (xPos[(int)steppingPositions.E7] + xPos[(int)steppingPositions.Dispense_to_Read]));
                moveY(yPos[(int)steppingPositions.Load] - (yPos[(int)steppingPositions.E7] + yPos[(int)steppingPositions.Dispense_to_Read]));
                
                //MessageBox.Show("Reading complete, displaying results");
                AutoClosingMessageBox.Show("Reading complete, displaying results", "Results", 2000);
                File.AppendAllText(logFilePath, "Reading complete, displaying results" + Environment.NewLine);

                // Remove In Progress Boxes and Show Test Results Grid
                inProgressBG_r.Visibility = Visibility.Hidden;
                inProgress_stack.Visibility = Visibility.Hidden;
                inProgress_carts.Visibility = Visibility.Hidden;
                inProgress_carts_borders.Visibility = Visibility.Hidden;

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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

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
                            digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
                            digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

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
                            digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
                            digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];
            string test2 = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[1];

            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
                                test2,
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
                        test2,
                        "port1",
                        ChannelLineGrouping.OneChannelForAllLines);

                    DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                    UInt32 data = reader.ReadSingleSamplePortUInt32();

                    //Update the Data Read box
                    string limitInputText = data.ToString();

                    if (limitInputText == "4")
                    {
                        for (int i = 0; i < 500; i++)
                        {
                            try
                            {
                                using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                                {
                                    //  Create an Digital Output channel and name it.
                                    digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

            for (int i = 0; i < v; i++)
            {
                try
                {
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
