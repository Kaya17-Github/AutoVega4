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

using NationalInstruments;
using NationalInstruments.DAQmx;
using Task = System.Threading.Tasks.Task;

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
        int drainTime = 180000; //wait for 3 minutes

        public MainWindow()
        {
            InitializeComponent();

            timeStamp = DateTime.Now.ToString("ddMMMyy_HHmmss");
            isReading = false;
        }

        private void operator_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (kit_tb != null && kit_tb.IsEnabled == false)
                {
                    kit_tb.IsEnabled = true;
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
                    reader_tb.IsEnabled = true;
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

            string logFilePath = @"C:\Users\Public\Documents\kaya17\log\kaya17-AutoVega_" + timeStamp + "_logfile.txt";
            File.AppendAllText(logFilePath, "Test type radiobuttons enabled" + Environment.NewLine);
        }

        private async void start_button_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Start button clicked");

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
                read_tb.Text = "Operator ID: " + operator_ID + Environment.NewLine + "Kit ID: " + kit_ID + Environment.NewLine
                    + "Reader ID: " + reader_ID + Environment.NewLine + "Test Type: " + test;

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)Allergy_rb.IsChecked == true)
            {
                string test = "Allergy";
                read_tb.Text = "Operator ID: " + operator_ID + Environment.NewLine + "Kit ID: " + kit_ID + Environment.NewLine
                    + "Reader ID: " + reader_ID + Environment.NewLine + "Test Type: " + test;

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }
            else if ((bool)Respiratory_rb.IsChecked == true)
            {
                string test = "Respiratory";
                read_tb.Text = "Operator ID: " + operator_ID + Environment.NewLine + "Kit ID: " + kit_ID + Environment.NewLine
                    + "Reader ID: " + reader_ID + Environment.NewLine + "Test Type: " + test;

                string appendOutputText = operator_ID + delimiter + kit_ID +
                delimiter + reader_ID + delimiter + test + delimiter + Environment.NewLine;
                File.AppendAllText(outputFilePath, appendOutputText);

                //MessageBox.Show("ID and test type (" + test + ") written to file");
                File.AppendAllText(logFilePath, "ID and test type (" + test + ") written to file" + Environment.NewLine);
            }

            // read parameter file and read in all necessary parameters
            string[] parameters = File.ReadAllLines(@"C:\Users\Public\Documents\kaya17\bin\Kaya17Covi2V.txt");
            string ledOutputRange = parameters[0].Substring(0, 3);
            string numSamplesPerReading = parameters[1].Substring(0, 3);
            string numTempSamplesPerReading = parameters[2].Substring(0, 2);
            string samplingRate = parameters[3].Substring(0, 5);
            string numSamplesForAvg = parameters[4].Substring(0, 3);
            string errorLimitInMillivolts = parameters[5].Substring(0, 2);
            string expectedDarkRdg = parameters[6].Substring(0, 4);
            string readMethod = parameters[9].Substring(0, 1);
            string ledOnDuration = parameters[9].Substring(4, 3);
            string readDelayInMS = parameters[9].Substring(8, 1);
            string excitationLedVoltage = parameters[33].Substring(0, 5);
            string afeScaleFactor = parameters[35].Substring(0, 5);
            string afeShiftFactor = parameters[36].Substring(0, 6);
            string viralCountScaleFactor = parameters[45].Substring(0, 5);
            string viralCountOffsetFactor = parameters[46].Substring(0, 4);
            string antigenCutoffFactor = parameters[47].Substring(0, 1);
            string antigenNoiseMargin = parameters[48].Substring(0, 1);
            string antigenControlMargin = parameters[49].Substring(0, 2);

            string bgd_rdg = afeShiftFactor;
            string threshold = viralCountOffsetFactor;

            //MessageBox.Show("Parameter file read");
            File.AppendAllText(logFilePath, "Parameter file read" + Environment.NewLine);

            // run instrument self-test
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
            Thread.Sleep(1000);

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

            MessageBox.Show("Open lid, load cartridge(s), close lid, enter patient name and number of samples, and press 'read array cartridge'");

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
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];
            string test2 = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[1];

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

                    if (LimitInputText == "2")
                    {
                        for (int i = 0; i < 50; i++)
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
                                    Thread.Sleep(5);
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

            // turns motor a specified amount of steps (x positive)
            for (int i = 0; i < 600; i++)
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 48);
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

                            if (LimitInputText == "1" || LimitInputText == "5")
                            {
                                MessageBox.Show("X limit reached");
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

            // steps x back so limit switch is not pressed
            for (int i = 0; i < 8; i++)
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            // turns motor a specified amount of steps (y positive)
            for (int i = 0; i < 1000; i++)
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 8);
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

                            if (LimitInputText == "2" || LimitInputText == "6")
                            {
                                MessageBox.Show("Y limit reached");
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

            // steps y back so limit switch is not pressed
            for (int i = 0; i < 50; i++)
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            // turn z motor until limit switch is reached (z positive)
            for (int i = 0; i < 10000; i++)
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
                        Thread.Sleep(1);
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

            // steps z back so limit switch is not pressed
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

        private void MoveToLoadPosition()
        {
            string test = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External)[0];

            // turns motor a specified amount of steps (x negative)
            // need to find how many steps
            for (int i = 0; i < 502; i++)
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            // turns motor a specified amount of steps (y negative)
            // need to find how many steps
            for (int i = 0; i < 825; i++)
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
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
            isReading = true;

            Ellipse[] thirtyWells = {a1, b1, c1, d1, e2, d2, c2, b2, a2, a3, b3, c3, d3, e3, e4, d4, c4, b4, a4,
                a5, b5, c5, d5, e5, e6, d6, c6, b6, a6, a7, b7, c7, d7, e7};

            Ellipse[] fourWells = { center_a1, center_a2, center_a3, center_a4 };

            if (sampleNum_tb.Text == "30")
            {
                A.Visibility = Visibility.Visible;
                B.Visibility = Visibility.Visible;
                C.Visibility = Visibility.Visible;
                D.Visibility = Visibility.Visible;
                E.Visibility = Visibility.Visible;
                tbA.Visibility = Visibility.Visible;
                tbB.Visibility = Visibility.Visible;
                tbC.Visibility = Visibility.Visible;
                tbD.Visibility = Visibility.Visible;
                tbE.Visibility = Visibility.Visible;
                tb1.Visibility = Visibility.Visible;
                tb2.Visibility = Visibility.Visible;
                tb3.Visibility = Visibility.Visible;
                tb4.Visibility = Visibility.Visible;
                tb5.Visibility = Visibility.Visible;
                tb6.Visibility = Visibility.Visible;
                tb7.Visibility = Visibility.Visible;
                MessageBox.Show("Reading 30 samples");
            }

            else if (sampleNum_tb.Text == "4")
            {
                center_A_tb.Visibility = Visibility.Visible;
                center_1_tb.Visibility = Visibility.Visible;
                center_2_tb.Visibility = Visibility.Visible;
                center_3_tb.Visibility = Visibility.Visible;
                center_4_tb.Visibility = Visibility.Visible;
                _4C_Stack.Visibility = Visibility.Visible;
                MessageBox.Show("Reading 4 samples");
            }

            string[] map = File.ReadAllLines(@"C:\Users\sri\OneDrive\Documents\Gokul\source\repos\Auto Vega\single_well_cartridge_map.csv");
            string[] positions = new string[map.Length];
            int[] xPos = new int[map.Length];
            int[] yPos = new int[map.Length];

            for (int i = 0; i < map.Length; i++)
            {
                positions[i] = map[i].Split(',')[0];
                xPos[i] = Int32.Parse(map[i].Split(',')[1]);
                yPos[i] = Int32.Parse(map[i].Split(',')[2]);
            }

            MessageBox.Show("Wait 3 minutes for samples to drain through cartridges");

            AutoClosingMessageBox.Show("Samples Draining", "Draining", drainTime);

            MessageBox.Show("Adding wash buffer to cartridges");

            MessageBox.Show("Drawing liquid from " + positions[0]);

            // Move to WB Bottle
            moveXPositive(xPos[0]);
            moveYPositive(yPos[0]);

            // Lower Z by 9000 steps
            lowerZPosition(9000);

            // Draw 1400 steps
            drawLiquid(1400);

            // Raise Z by 9000 steps
            raiseZPosition(9000);

            MessageBox.Show("Dispensing WB in " + positions[2]);

            // Move to Cartridge 1
            moveXNegative(Math.Abs(xPos[2] - xPos[0]));
            moveYNegative(Math.Abs(yPos[2] - yPos[0]));

            // Lower Z by 2000 steps
            lowerZPosition(2000);

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a1.Fill = Brushes.Green;

            MessageBox.Show("Dispensing WB in " + positions[3]);

            // Move to Cartridge 2
            moveYPositive(Math.Abs(yPos[3] - yPos[2]));

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a2.Fill = Brushes.Green;

            MessageBox.Show("Dispensing WB in " + positions[4]);

            // Move to Cartridge 3
            moveYPositive(Math.Abs(yPos[4] - yPos[3]));

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a3.Fill = Brushes.Green;

            MessageBox.Show("Dispensing WB in " + positions[5]);

            // Move to Cartridge 4
            moveYPositive(Math.Abs(yPos[5] - yPos[4]));

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a4.Fill = Brushes.Green;

            // Raise Z by 2000 steps
            raiseZPosition(2000);

            MessageBox.Show("Moving back to load position");

            // Move back to Load Position
            moveXNegative(Math.Abs(xPos[6] - xPos[5]));
            moveYNegative(Math.Abs(yPos[6] - yPos[5]));

            MessageBox.Show("Wait 3 minutes for wash buffer to drain through cartridges");

            AutoClosingMessageBox.Show("Wash Buffer Draining", "Draining", drainTime);

            MessageBox.Show("Remove absorbent pads and reload cartridges");

            MessageBox.Show("Adding read buffer to cartridges");

            // Move to RB Bottle
            moveXPositive(xPos[1]);
            moveYPositive(yPos[1]);

            // Lower Z by 9000 steps
            lowerZPosition(9000);

            // Draw 1400 steps
            drawLiquid(1400);

            // Raise Z by 9000 steps
            raiseZPosition(9000);

            // Move to Cartridge 1
            moveXPositive(Math.Abs(xPos[2] - xPos[1]));
            moveYNegative(Math.Abs(yPos[2] - yPos[1]));

            // Lower Z by 2000 steps
            lowerZPosition(2000);

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a1.Fill = Brushes.Green;

            // Move to Cartridge 2
            moveYPositive(Math.Abs(yPos[3] - yPos[2]));

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a2.Fill = Brushes.Green;

            // Move to Cartridge 3
            moveYPositive(Math.Abs(yPos[4] - yPos[3]));

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a3.Fill = Brushes.Green;

            // Move to Cartridge 4
            moveYPositive(Math.Abs(yPos[5] - yPos[4]));

            // Dispense 350 steps
            dispenseLiquid(350);

            center_a4.Fill = Brushes.Green;

            // Raise Z by 2000 steps
            raiseZPosition(2000);

            MessageBox.Show("Reading samples in all wells");

            // Add reading code here


            // Display results


            // Move back to Load Position
            moveXNegative(Math.Abs(xPos[6] - xPos[5]));
            moveYNegative(Math.Abs(yPos[6] - yPos[5]));


            isReading = false;
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

        private void moveYPositive(int v)
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
                        writer.WriteSingleSamplePort(true, 12);
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 8);
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 2);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void moveXNegative(int v)
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
                        writer.WriteSingleSamplePort(true, 16);
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
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
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
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

        private void moveYNegative(int v)
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
                        writer.WriteSingleSamplePort(true, 4);
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void moveXPositive(int v)
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
                        writer.WriteSingleSamplePort(true, 32);
                        Thread.Sleep(5);
                        writer.WriteSingleSamplePort(true, 48);
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
    }
}
