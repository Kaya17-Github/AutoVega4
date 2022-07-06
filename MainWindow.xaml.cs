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
        string timeStamp;
        string testTime;
        int drainTime = 300000; //wait for 4 minutes
        int wait = 0;

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

            timeStamp = DateTime.Now.ToString("ddMMMyy_HHmmss");
            testTime = DateTime.Now.ToString("ddMMM_HHmm");
            isReading = false;            

            InitializeComponent();

            // testRead();
        }

        // To test Reader code
        private void testRead()
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
            StringBuilder sb = new StringBuilder(5000);
            bool initializeBoardBool = testInitializeBoard(sb, sb.Capacity);

            MessageBox.Show("Test initializeBoard: " + initializeBoardBool + "\n" + sb.ToString());

            // MessageBox.Show("Insert Cartridge and then click ok");

            StringBuilder sb2 = new StringBuilder(10000);

            IntPtr testBoardValuePtr = testGetBoardValue(sb2, sb2.Capacity);
            double[] testArray3 = new double[5];
            Marshal.Copy(testBoardValuePtr, testArray3, 0, 5);
            testString = "";
            testString += "Return Value: m_dAvgValue = " + testArray3[0] + "\n";
            testString += "Return Value: m_dCumSum = " + testArray3[1] + "\n";
            testString += "Return Value: m_dLEDtmp = " + testArray3[2] + "\n";
            testString += "Return Value: m_dPDtmp = " + testArray3[3] + "\n";
            testString += "Return Value: testGetBoardValue = " + testArray3[4] + "\n";

            MessageBox.Show(testString + sb2.ToString());

            testCloseTasksAndChannels();
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

            string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";
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

            string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";
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

                if (Respiratory_rb != null && Respiratory_rb.IsEnabled == false)
                {
                    Respiratory_rb.IsEnabled = true;
                }
            }
            catch { }

            testname_tb.Text = "Kaya17-AutoVega_" + testTime;
            testname_tb.FontSize = 16;

            string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";
            File.AppendAllText(logFilePath, "Test type radiobuttons enabled" + Environment.NewLine);
        }

        private async void start_button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Start button clicked");

            string outputFilePath = @"C:\Users\Public\Documents\Kaya17\Data\kaya17-AutoVega_" + timeStamp + ".csv";
            string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";

            string delimiter = ",";

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
            else if ((bool)Respiratory_rb.IsChecked == true)
            {
                string test = "Respiratory";

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }

            /*// run instrument self-test
            message_tb.Text = "Running Initialization";
            MessageBox.Show("Running Initialization");
            File.AppendAllText(logFilePath, "Running Initialization" + Environment.NewLine);

            MessageBox.Show("Test in progress. Do not open lid.");

            // **This is here just for testing**
            Thread.Sleep(1000);

            message_tb.Text = "Initialization Complete";
            MessageBox.Show("Initialization Complete");
            File.AppendAllText(logFilePath, "Initialization Complete" + Environment.NewLine);

            // **This is here just for testing**
            Thread.Sleep(1000);*/

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

            MessageBox.Show("Moving to home position");
            message_tb.Text = "Moving to home position";
            File.AppendAllText(logFilePath, "Moving to home position" + Environment.NewLine);

            MoveToHomePosition();



            MessageBox.Show("Cartridge at home position");
            message_tb.Text = "Cartridge at home position";
            File.AppendAllText(logFilePath, "Cartridge at home position" + Environment.NewLine);

            // **This is here just for testing**
            Thread.Sleep(1000);

            MessageBox.Show("Moving to load position");
            message_tb.Text = "Moving to load position";
            File.AppendAllText(logFilePath, "Moving to load position" + Environment.NewLine);

            MoveToLoadPosition();

            MessageBox.Show("Cartridge at load position");
            message_tb.Text = "Cartridge at load position";
            File.AppendAllText(logFilePath, "Cartridge at load position" + Environment.NewLine);

            MessageBox.Show("Open lid, load cartridge(s), close lid, enter patient name and number of samples," +
                " and press 'read array cartridge' after cartridge is aligned.");

            // TODO: Check for cartridge alignment

            MessageBox.Show("Cartridge aligned");
            File.AppendAllText(logFilePath, "Cartridge aligned" + Environment.NewLine);

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
            Respiratory_rb.IsEnabled = false;

            File.AppendAllText(logFilePath, "Operator id, kit id, reader id, and test type radiobuttons disabled" + Environment.NewLine);
        }

        private void MoveToHomePosition()
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

            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];
            string test2 = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[1];            

            // turns motor a specified amount of steps (x negative)
            for (int i = 0; i < xPos[0]; i++)
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
            for (int i = 0; i < xPos[1]; i++)
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

            // turns motor a specified amount of steps (y negative)
            for (int i = 0; i < yPos[0]; i++)
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
            for (int i = 0; i < yPos[1]; i++)
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

            // turn z motor until limit switch is reached (z positive)
            for (int i = 0; i < zPos[0]; i++)
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
                        Thread.Sleep(wait);
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
            for (int i = 0; i < zPos[1]; i++)
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

            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

            // turns motor y positive
            for (int i = 0; i < yPos[2]; i++)
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

            /*// turns motor x positive
            for (int i = 0; i < xPos[2]; i++)
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
            }*/
        }

        private async void read_button_Click(object sender, RoutedEventArgs e)
        {
            if (sampleNum_tb.Text == "4")
            {
                string inProgressColor = "#F5D5CB";
                string finishedColor = "#D7ECD9";

                string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";

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

                string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";

                // define output port for switching port 2 banks
                string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];
                string test3 = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[2];
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

                Ellipse[] inProgressEllipses = {inProgressA2,inProgressA3,inProgressA4,inProgressA5,inProgressA6,inProgressA7,
                                                inProgressB2,inProgressB3,inProgressB4,inProgressB5,inProgressB6,inProgressB7,
                                                inProgressC2,inProgressC3,inProgressC4,inProgressC5,inProgressC6,inProgressC7,
                                                inProgressD2,inProgressD3,inProgressD4,inProgressD5,inProgressD6,inProgressD7,
                                                inProgressE2,inProgressE3,inProgressE4,inProgressE5,inProgressE6,inProgressE7};

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

                string[] map = File.ReadAllLines("../../Auto Vega/34_well_cartridge_steps.csv");
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

                int drainZ = zPos[4];
                int drawZ = zPos[5];
                int drawPump = pump[5];
                int dispensePump = pump[5];
                int dispenseToReadX = 2400;
                int dispenseToReadY = 275;

                //MessageBox.Show("Moving to Drain Position");
                AutoClosingMessageBox.Show("Moving to Drain Position", "Moving", 3000);
                File.AppendAllText(logFilePath, "Moving to Drain Position" + Environment.NewLine);

                // Move to Drain Position
                moveX(xPos[4] - xPos[3]);
                moveY(yPos[4] - yPos[3]);

                // Change Sample Draining Box and Cartridges to in progress color
                sampleDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                sampleDrain_tb.Foreground = Brushes.Black;
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Lower Pipette Tips to Drain
                lowerZPosition(drainZ);

                //MessageBox.Show("Wait 5 minutes for samples to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 5 minutes for samples to drain through cartridges", "Draining", 3000);
                File.AppendAllText(logFilePath, "Wait 5 minutes for samples to drain through cartridges" + Environment.NewLine);

                // Turn pump on
                try
                {
                    // Switch to bank 2
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
                    // Send signal to turn on pump
                    using (NationalInstruments.DAQmx.Task digitalWriteTask = new NationalInstruments.DAQmx.Task())
                    {
                        //  Create an Digital Output channel and name it.
                        digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
                        digitalWriteTask.DOChannels.CreateChannel(test, "port0",
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
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                //MessageBox.Show("Sample Draining Complete");
                AutoClosingMessageBox.Show("Sample Draining Complete", "Draining Complete", 3000);
                File.AppendAllText(logFilePath, "Sample Draining Complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(drainZ);

                // Move to WB Bottle
                moveX(xPos[5] - xPos[4]);
                moveY(yPos[5] - yPos[4]);

                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Draw WB for first 2 rows
                AutoClosingMessageBox.Show("Drawing liquid from wash buffer bottle", "Drawing WB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from wash buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 3500 steps (5mL)
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Foreground = Brushes.Black;

                // Dispense 500ul WB in First 2 Rows
                for (int i = 7; i < 17; i++)
                {
                    if (i == 7)
                    {
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 3000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 7)
                    {
                        moveX(xPos[i] - xPos[5]);
                        moveY(yPos[i] - yPos[5]);
                    }
                    else
                    {
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Move back to WB bottle
                moveX(xPos[5] - xPos[16]);
                moveY(yPos[5] - yPos[16]);

                // Draw WB for third and fourth rows
                AutoClosingMessageBox.Show("Drawing liquid from wash buffer bottle", "Drawing WB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from wash buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 3500 steps (5mL)
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                // Dispense 500ul WB in third and fourth Rows
                for (int i = 17; i < 27; i++)
                {
                    if (i == 17)
                    {
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 3000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 17)
                    {
                        moveX(xPos[i] - xPos[5]);
                        moveY(yPos[i] - yPos[5]);
                    }
                    else
                    {
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Move back to WB bottle
                moveX(xPos[5] - xPos[26]);
                moveY(yPos[5] - yPos[26]);

                // Draw WB for fifth and sixth rows
                AutoClosingMessageBox.Show("Drawing liquid from wash buffer bottle", "Drawing WB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from wash buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 3500 steps (5mL)
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                // Dispense 500ul WB in fifth and sixth Rows
                for (int i = 27; i < 37; i++)
                {
                    if (i == 27)
                    {
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing wash buffer in " + positions[i], "Dispensing WB", 3000);
                    File.AppendAllText(logFilePath, "Dispensing wash buffer in " + positions[i] + Environment.NewLine);

                    if (i == 27)
                    {
                        moveX(xPos[i] - xPos[5]);
                        moveY(yPos[i] - yPos[5]);
                    }
                    else
                    {
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                inProgressEllipses[29].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                //MessageBox.Show("WB Dispensed");
                AutoClosingMessageBox.Show("Wash Buffer Dispensed", "Dispensing Complete", 3000);
                File.AppendAllText(logFilePath, "Wash Buffer Dispensed" + Environment.NewLine);

                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                //MessageBox.Show("Moving Back to Drain Position");
                AutoClosingMessageBox.Show("Moving Back to Drain Position", "Moving", 3000);
                File.AppendAllText(logFilePath, "Moving Back to Drain Position" + Environment.NewLine);

                // Move back to Drain Position
                moveX(xPos[4] - xPos[36]);
                moveY(yPos[4] - yPos[36]);

                wbDrain_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDrain_tb.Foreground = Brushes.Black;
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                }

                // Lower Pipette Tips to Drain
                lowerZPosition(drainZ);

                //MessageBox.Show("Wait 5 minutes for WB to drain through cartridges");
                AutoClosingMessageBox.Show("Wait 5 minutes for WB to drain through cartridges", "Draining", 3000);
                File.AppendAllText(logFilePath, "Wait 5 minutes for WB to drain through cartridges" + Environment.NewLine);

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

                // Leave pump on for 5 minutes
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
                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                }

                //MessageBox.Show("WB Draining Complete");
                AutoClosingMessageBox.Show("Wash buffer draining complete", "Draining Complete", 3000);
                File.AppendAllText(logFilePath, "Wash buffer draining complete" + Environment.NewLine);

                // Lift Pipette Tips above top of bottles
                raiseZPosition(drainZ);

                // Move to RB Bottle
                moveX(xPos[6] - xPos[4]);
                moveY(yPos[6] - yPos[4]);

                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // Draw RB for first 2 rows
                AutoClosingMessageBox.Show("Drawing liquid from read buffer bottle", "Drawing RB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from read buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 3500 steps (5mL)
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                wbDispense_tb.Foreground = Brushes.Black;

                // Dispense 500ul RB in First 2 Rows
                for (int i = 7; i < 17; i++)
                {
                    if (i == 7)
                    {
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 3000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 7)
                    {
                        moveX(xPos[i] - xPos[6]);
                        moveY(yPos[i] - yPos[6]);
                    }
                    else
                    {
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Move back to RB bottle
                moveX(xPos[6] - xPos[16]);
                moveY(yPos[6] - yPos[16]);

                // Draw RB for third and fourth rows
                AutoClosingMessageBox.Show("Drawing liquid from read buffer bottle", "Drawing RB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from read buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 3500 steps (5mL)
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                // Dispense 500ul RB in third and fourth Rows
                for (int i = 17; i < 27; i++)
                {
                    if (i == 17)
                    {
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 3000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 17)
                    {
                        moveX(xPos[i] - xPos[6]);
                        moveY(yPos[i] - yPos[6]);
                    }
                    else
                    {
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                // Move back to RB bottle
                moveX(xPos[6] - xPos[26]);
                moveY(yPos[6] - yPos[26]);

                // Draw RB for fifth and sixth rows
                AutoClosingMessageBox.Show("Drawing liquid from read buffer bottle", "Drawing RB", 3000);
                File.AppendAllText(logFilePath, "Drawing liquid from read buffer bottle" + Environment.NewLine);

                // Lower Z by 9000 steps
                lowerZPosition(drawZ);

                // Draw 3500 steps (5mL)
                drawLiquid(drawPump);

                // Raise Z by 9000 steps
                raiseZPosition(drawZ);

                // Dispense 500ul RB in fifth and sixth Rows
                for (int i = 27; i < 37; i++)
                {
                    if (i == 27)
                    {
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }
                    else
                    {
                        inProgressEllipses[i - 8].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                        inProgressEllipses[i - 7].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                    }

                    AutoClosingMessageBox.Show("Dispensing read buffer in " + positions[i], "Dispensing RB", 3000);
                    File.AppendAllText(logFilePath, "Dispensing read buffer in " + positions[i] + Environment.NewLine);

                    if (i == 27)
                    {
                        moveX(xPos[i] - xPos[6]);
                        moveY(yPos[i] - yPos[6]);
                    }
                    else
                    {
                        moveX(xPos[i] - xPos[i - 1]);
                        moveY(yPos[i] - yPos[i - 1]);
                    }

                    dispenseLiquid(pump[i]);
                }

                inProgressEllipses[29].Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);

                wbDispense_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);
                wbDispense_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(finishedColor);                

                //MessageBox.Show("RB Dispensed");
                AutoClosingMessageBox.Show("Read buffer dispensed", "Dispensing Complete", 3000);
                File.AppendAllText(logFilePath, "Read buffer dispensed" + Environment.NewLine);

                for (int i = 0; i < inProgressEllipses.Length; i++)
                {
                    inProgressEllipses[i].Fill = Brushes.Gray;
                }

                // TODO: Check for lid closed
                MessageBox.Show("Please make sure lid is closed before continuing.");

                reading_border.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_border.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(inProgressColor);
                reading_tb.Foreground = Brushes.Black;

                //MessageBox.Show("Reading samples in all wells");
                AutoClosingMessageBox.Show("Reading samples in all wells", "Reading", 3000);
                File.AppendAllText(logFilePath, "Reading samples in all wells" + Environment.NewLine);

                // TODO: Add 30 well reading code here

                // Move to A1 Reading
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
