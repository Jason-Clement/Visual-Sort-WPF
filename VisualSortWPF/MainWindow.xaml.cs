using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Reflection;
using NAudio.Wave;
using NAudio.Midi;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace VisualSortWPF
{
    public partial class MainWindow : Window
    {
        private double[] _Notes = new double[]
        {
            27.5, 29.1353, 30.8677, 32.7032, 34.6479, 36.7081, 38.8909, 41.2035, 43.6536, 46.2493,
            48.9995, 51.913, 55, 58.2705, 61.7354, 65.4064, 69.2957, 73.4162, 77.7817, 82.4069,
            87.3071, 92.4986, 97.9989, 103.826, 110, 116.541, 123.471, 130.813, 138.591, 146.832,
            155.563, 164.814, 174.614, 184.997, 195.998, 207.652, 220, 233.082, 246.942, 261.626,
            277.183, 293.665, 311.127, 329.628, 349.228, 369.994, 391.995, 415.305, 440, 466.164,
            493.883, 523.251, 554.365, 587.33, 622.254, 659.255, 698.456, 739.989, 783.991, 830.609,
            880, 932.328, 987.767, 1046.5, 1108.73, 1174.66, 1244.51, 1318.51, 1396.91, 1479.98,
            1567.98, 1661.22, 1760, 1864.66, 1975.53, 2093, 2217.46, 2349.32, 2489.02, 2637.02,
            2793.83, 2959.96, 3135.96, 3322.44, 3520, 3729.31, 3951.07, 4186.01
        };

        private int[,] _ScaledNotes;

        private int[] _Bars;
        private Rectangle[] _Rectangles;
        private int _BarCount = 40;
        private int _Speed = 990;
        private string _AlgorithmName;

        private bool _Active = false;
        private SortingAlgorithm<int> _ActiveAlgorithm;
        private bool _Sorted = true;

        private bool _Cancel = false;

        private MidiOut _MidiOut;
        private DirectSoundOut _WaveOut;
        private SineWaveProvider32 _WaveProvider;
        private int _MaxFrequency;
        private int _MinFrequency;

        private int _SwapCount;
        private int _CompareCount;

        public MainWindow()
        {
            InitializeComponent();
            _WaveProvider = new SineWaveProvider32();
            _WaveProvider.SetWaveFormat(16000, 1);
            _WaveProvider.Amplitude = 0.25f;
            _WaveOut = new DirectSoundOut();
            _WaveOut.Init(_WaveProvider);

            _MidiOut = new MidiOut(0);
        }

        private void RandomizeBars(bool force)
        {
            if (force) _Sorted = true;
            RandomizeBars();
        }

        private int _RandomType = 0;
        private void RandomizeBars()
        {
            if (!_Sorted) return;
            int c, i, j, k, h, t, sec;
            _Sorted = false;
            _Bars = new int[_BarCount];
            _Rectangles = new Rectangle[_BarCount];
            _Canvas.Children.Clear();
            Random rand = new Random();
            for (i = 0; i < _BarCount; i++)
            {
                _Bars[i] = i + 1;
                _Rectangles[i] = new Rectangle();
                _Canvas.Children.Add(_Rectangles[i]);
            }
            switch (_RandomType)
            {
                case 0:
                    for (i = _BarCount; i > 1; i--)
                    {
                        j = rand.Next(i);
                        t = _Bars[j];
                        _Bars[j] = _Bars[i - 1];
                        _Bars[i - 1] = t;
                    }
                    break;
                case 1:
                    for (i = 0; i < _BarCount; i++)
                    {
                        _Bars[i] = _BarCount - i;
                    }
                    break;
                case 2:
                    sec = Math.Min(10, _BarCount / 5);
                    for (i = 1; i < _BarCount; i += sec)
                    {
                        for (j = Math.Min(i + sec, _BarCount); j > i; j--)
                        {
                            k = rand.Next(i, j);
                            t = _Bars[k];
                            _Bars[k] = _Bars[i - 1];
                            _Bars[i - 1] = t;
                        }
                    }
                    break;
                case 3:
                    sec = Math.Min(10, _BarCount / 10);
                    h = _BarCount / sec;
                    c = h - 1;
                    for (i = 0; i < _BarCount; i++)
                    {
                        if (i > 0 && i % h == 0)
                            c += h;
                        _Bars[i] = c + 1;
                    }
                    for (i = _BarCount; i > 1; i--)
                    {
                        j = rand.Next(i);
                        t = _Bars[j];
                        _Bars[j] = _Bars[i - 1];
                        _Bars[i - 1] = t;
                    }
                    break;
            }
            _AlgorithmName = "";
            DrawIt();
        }

        private void Sleep()
        {
            Thread.Sleep(1000 - (int)((_Speed / 2.0 + 50) * 10));
        }

        private bool _PlayOnCompare;
        private bool _PlayOnSwap;

        private void BeginSort(SortingAlgorithm<int> algorithm)
        {
            if (_Active)
                _ActiveAlgorithm.Cancel();
            _Active = true;
            _ActiveAlgorithm = algorithm;
            _StopButton.Visibility = System.Windows.Visibility.Visible;
            _PlayButton.Visibility = System.Windows.Visibility.Collapsed;
            _ScrambleButton.IsEnabled = false;
            _BarCountBox.IsEnabled = false;
            _ActiveAlgorithm.Complete += (s2, e2) =>
            {
                EndSort();
                _Sorted = true;
            };
            RandomizeBars();
            _AlgorithmName = algorithm.Name;
            _AlgorithmNameTextBlock.Text = _AlgorithmName;
            _SwapCount = 0;
            _SwapCountTextBlock.Text = "0";
            _CompareCount = 0;
            _CompareCountTextBlock.Text = "0";
            //_WaveOut.Play();
            _ActiveAlgorithm.ActiveRangeChange += (s, e) =>
            {
                UpdateActiveRange(e.Items);
            };
            _ActiveAlgorithm.ItemsSwapped += (s, e) =>
            {
                _SwapCount++;
                this.Dispatcher.BeginInvoke(new Action(() => { _SwapCountTextBlock.Text = _SwapCount.ToString(); }));
                UpdateRectangleHeight(e.Item1Index, e.Item1);
                UpdateRectangleHeight(e.Item2Index, e.Item2);
                if (_PlayOnSwap)
                {
                    PlaySound(e.Item1 - 1);
                    PlaySound(e.Item2 - 1);
                }
            };
            _ActiveAlgorithm.ItemsCompared += (s, e) =>
            {
                _CompareCount++;
                this.Dispatcher.BeginInvoke(new Action(() => { _CompareCountTextBlock.Text = _CompareCount.ToString(); }));
                UpdateIndicator(0, e.Item1Index);
                UpdateIndicator(1, e.Item2Index);
                if (_PlayOnCompare)
                {
                    PlaySound(e.Item1 - 1);
                    PlaySound(e.Item2 - 1);
                }
            };
            _ActiveAlgorithm.ItemUpdated += (s, e) =>
            {
                UpdateRectangleHeight(e.ItemIndex, e.Item);
            };
            foreach (Polygon indicator in _Indicators)
                indicator.Visibility = System.Windows.Visibility.Visible;
            _ActiveAlgorithm.Sort(_Bars);
        }

        private void EndSort()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                _StopButton.Visibility = System.Windows.Visibility.Collapsed;
                _PlayButton.Visibility = System.Windows.Visibility.Visible;
                _ScrambleButton.IsEnabled = true;
                _BarCountBox.IsEnabled = true;
                _Active = false;
                foreach (Polygon indicator in _Indicators)
                    indicator.Visibility = System.Windows.Visibility.Hidden;
                DrawIt();
                _Cancel = false;
            }));
           // _WaveOut.Stop();
        }

        private int _SoundType = 1;

        private void PlaySound(params int[] values)
        {
            if (_SoundType == 1)
                PlayMidiSound(values);
        }

        private ConcurrentQueue<Tuple<int, int>> _MidiNotes;
        private int _MidiChannel;
        private int _MaxNotes;
        private void PlayMidiSound(params int[] values)
        {
            if (_MidiNotes == null)
                _MidiNotes = new ConcurrentQueue<Tuple<int, int>>();
            for (int i = 0; i < values.Length; i++)
            {
                //_MidiOut.Send(MidiMessage.StartNote((int)_ScaledNotes[values[i], 0], 127, _MidiChannel++).RawData);
                _MidiOut.Send(MidiMessage.StartNote((int)_ScaledNotes[values[i], 0] + 20, 127, 0).RawData);
                //_MidiNotes.Enqueue(new Tuple<int, int>(_ScaledNotes[values[i], 0], _MidiChannel));
                if (++_MidiChannel > 16)
                    _MidiChannel = 0;
                //if (_MidiNotes.Count > _MaxNotes)
                //{
                //    Tuple<int, int> note;
                //    while (!_MidiNotes.TryDequeue(out note)) ;
                //    _MidiOut.Send(MidiMessage.StopNote(note.Item1, 0, note.Item2).RawData);
                //}
            }
        }



        private double _BarWidth;
        private double _IndicatorWidth;
        private List<Polygon> _Indicators;

        private void UpdateActiveRange(params int[] bars)
        {
            if (_Cancel) return;
            this.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < _Bars.Length; i++)
                    {
                        if (bars.Contains(i))
                            _Rectangles[i].Fill = Brushes.Red;
                        else
                            _Rectangles[i].Fill = Brushes.Blue;
                    }
                }
            ));
        }

        /*
        private void UpdateIndicators(params Bar[] bars)
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    int i = 0;
                    //float[] frequencies = new float[bars.Length];
                    for (; i < bars.Length; i++)
                    {
                        if (_MidiOut != null)
                        {
                            _MidiOut.Send(MidiMessage.StartNote((int)_ScaledNotes[bars[i].Value - 1, 0] + 20, 127, 0).RawData);
                            if (_ScaledNotes[bars[i].Value - 1, 1] > 0)
                            {
                                _MidiOut.Send(new MidiMessage((int)MidiCommandCode.PitchWheelChange, 0, _ScaledNotes[bars[i].Value -1, 1]).RawData);
                            }
                        }
                        //frequencies[i] = (float)bars[i].Value / (float)_BarCount * (_MaxFrequency - _MinFrequency) + _MinFrequency;
                        //frequencies[i] = (float)_Notes[(int)(((float)bars[i].Value - 1f) / (float)_BarCount * _Notes.Length)];
                        if (_Indicators.Count <= i)
                        {
                            Polygon polygon = new Polygon();
                            polygon.Points = new PointCollection(new Point[] {
                                new Point(0, _IndicatorCanvas.ActualHeight - 1),
                                new Point(_IndicatorWidth / 2, 1),
                                new Point(_IndicatorWidth, _IndicatorCanvas.ActualHeight - 1)});
                            polygon.Fill = Brushes.Red;
                            _Indicators.Add(polygon);
                            _IndicatorCanvas.Children.Add(polygon);
                            Canvas.SetTop(_Indicators[i], 1);
                        }
                        _Indicators[i].Visibility = System.Windows.Visibility.Visible;
                        Canvas.SetLeft(_Indicators[i], bars[i].Index * _BarWidth - 3 + _BarWidth / 2);
                    }
                    for (; i < _Indicators.Count; i++)
                    {
                        _Indicators[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    //if (_WaveProvider != null && _PlayOnCompare.IsChecked == true)
                    //    _WaveProvider.AddFrequencies(frequencies);
                }
            ));
        }
         * */

        private void UpdateIndicator(int index, int value)
        {
            if (_Cancel) return;
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                //_Indicators[index].Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(_Indicators[index], value * _BarWidth - 3 + _BarWidth / 2);
            }));
        }

        /*
        private void UpdateRectangleHeights(params Bar[] bars)
        {
            if (_Cancel) return;
            this.Dispatcher.Invoke(new Action(() =>
            {
                float[] frequencies = new float[bars.Length];
                for (int i = 0; i < bars.Length; i++)
                {
                    frequencies[i] = (float)bars[i].Value / (float)_BarCount * (_MaxFrequency - _MinFrequency) + _MinFrequency;
                    _Rectangles[bars[i].Index].Height = _Canvas.ActualHeight * ((double)bars[i].Value / (double)_BarCount);
                    Canvas.SetTop(_Rectangles[bars[i].Index], _Canvas.ActualHeight - _Rectangles[bars[i].Index].Height);
                }
                //if (_WaveProvider != null && _PlayOnSwap.IsChecked == true)
                //    _WaveProvider.AddFrequencies(frequencies);
            }
            ));
        }
         * */
        private void UpdateRectangleHeight(int index, int height)
        {
            if (_Cancel) return;

            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _Rectangles[index].Height = _Canvas.ActualHeight * ((double)height / (double)_BarCount);
                    Canvas.SetTop(_Rectangles[index], _Canvas.ActualHeight - _Rectangles[index].Height);
                }));
        }

        private void DrawIt(params int[] current)
        {
            DrawIt(new int[] { }, current);
        }

        public void DrawIt(int[] work, params int[] current)
        {
            double margin = 0;
            double drawHeight = _Canvas.ActualHeight;
            double drawWidth = _Canvas.ActualWidth;
            double barWidth = drawWidth / _BarCount;
            double barSpace = barWidth * 0.4;
            if (barSpace < 2.4) barSpace = -1.0;
            for (int i = 0; i < _BarCount; i++)
            {
                double y = _Canvas.ActualHeight - (double)_Bars[i] / (double)_BarCount * drawHeight;
                Rectangle r = _Rectangles[i];
                double width = barWidth - barSpace / 2;
                if (width < 0) width = barWidth;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    r.Fill = Brushes.Blue;
                    r.Height = drawHeight * ((double)_Bars[i] / (double)_BarCount);
                    r.Width = width;
                    Canvas.SetLeft(r, barWidth * i + barSpace / 2);
                    Canvas.SetTop(r, y - margin);
                }));
            }
            this.Dispatcher.Invoke(new Action(() => { _Canvas.UpdateLayout(); }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _Indicators = new List<Polygon>();
            for (int i = 0; i < 2; i++)
            {
                Polygon polygon = new Polygon();
                polygon.Points = new PointCollection(new Point[] {
                                    new Point(0, _IndicatorCanvas.ActualHeight - 1),
                                    new Point(_IndicatorWidth / 2, 1),
                                    new Point(_IndicatorWidth, _IndicatorCanvas.ActualHeight - 1)});
                polygon.Fill = Brushes.Red;
                polygon.Visibility = System.Windows.Visibility.Hidden;
                _Indicators.Add(polygon);
                _IndicatorCanvas.Children.Add(polygon);
                Canvas.SetTop(_Indicators[i], 1);
            }
            InitCanvas();
            RandomizeBars(true);
            DrawIt();
        }

        private void _Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitCanvas();
            DrawIt();
        }

        private void InitCanvas()
        {
            _BarWidth = _Canvas.ActualWidth / _BarCount;
            _IndicatorWidth = 8;
            _BarCountBox.Maximum = (int)_Canvas.ActualWidth;
        }

        private Expander _ActiveExpander;
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (_ActiveExpander != null && _ActiveExpander != (Expander)sender)
            {
                _ActiveExpander.IsExpanded = false;
            }
            _ActiveExpander = (Expander)sender;
        }

        private void SortTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            SortingAlgorithm<int> sortingAlgorithm = SortingAlgorithm<int>.FromName(textBlock.Text);
            BeginSort(sortingAlgorithm);
        }

        private double _BarSpace;
        private void BarCountBoxChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            _BarCount = (int)e.NewValue;
            _BarWidth = _Canvas.ActualWidth / _BarCount;
            _BarSpace = _BarWidth * 0.2;
            RandomizeBars(true);

            double scale = (double)_Notes.Length / (double)(_BarCount - 1);
            _ScaledNotes = new int[_BarCount, 2];
            int z = -1;
            int c = 0;
            for (int i = 0; i < _BarCount; i++)
            {
                int k = (int)(i * scale);
                _ScaledNotes[i, 0] = k;
                _ScaledNotes[i, 1] = 0;
                if (z != k)
                {
                    z = k;
                    if (i > 0 && c > 1)
                    {
                        double step = 0x20d / (double)c;
                        for (int j = 1; j < c; j++)
                        {
                            _ScaledNotes[i - c + j, 1] = (int)(0x3F + j * step);
                        }
                    }
                    c = 1;
                }
                else
                {
                    c++;
                }
            }
        }

        private void SpeedBoxChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            SortingAlgorithm<int>.Delay = (int)e.NewValue;
        }

        private void _ScambleButton_Click(object sender, RoutedEventArgs e)
        {
            RandomizeBars(true);
        }

        private void _StopButton_Click(object sender, RoutedEventArgs e)
        {
            _Cancel = true;
            _ActiveAlgorithm.Cancel();
            EndSort();
        }

        private void _ConcurrentNotes_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (_WaveProvider != null)
                _WaveProvider.MaxFrequencies = (int)e.NewValue;
            _MaxNotes = (int)e.NewValue;
        }

        private void _MinFrequencyBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            _MinFrequency = (int)e.NewValue;
        }

        private void _MaxFrequencyBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            _MaxFrequency = (int)e.NewValue;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_MidiOut != null)
                _MidiOut.Dispose();
            if (_WaveOut != null)
                _WaveOut.Dispose();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            ComboBox cb = sender as ComboBox;
            if (_SineWaveOptions == null || _MidiOptions == null)
                return;
            if (cb.SelectedIndex == 0)
            {
                if (_MidiOptions.Visibility == System.Windows.Visibility.Collapsed)
                {
                    _SineWaveOptions.Visibility = System.Windows.Visibility.Collapsed;
                    _MidiOptions.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                if (_SineWaveOptions.Visibility == System.Windows.Visibility.Collapsed)
                {
                    _MidiOptions.Visibility = System.Windows.Visibility.Collapsed;
                    _SineWaveOptions.Visibility = System.Windows.Visibility.Visible;
                }
            }
             
        }

        private void _MidiInstrument_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            
            for (int i = 0; i < 16; i++)
                _MidiOut.Send(MidiMessage.ChangePatch((int)_MidiInstrument.Value, i).RawData);
             
        }

        private void _SO_PlayOnCompare_Checked(object sender, RoutedEventArgs e)
        {
            _PlayOnCompare = _SO_PlayOnCompare.IsChecked == true;
        }

        private void _SO_PlayOnSwap_Checked(object sender, RoutedEventArgs e)
        {
            _PlayOnSwap = _SO_PlayOnSwap.IsChecked == true;
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            _RandomType = ((ComboBox)sender).SelectedIndex;
            RandomizeBars(true);
        }
    }
}
