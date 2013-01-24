using System;
using System.IO;
using System.Threading;
using MediaDemo.SoundTouch;
using NAudio.Wave;

namespace MediaDemo
{
    public enum AudioState
    {
        Uninitialized   = 0,

        Prepared        = 1,
        
        Playing         = 2,
        
        Pausing         = 3,
        
        Stopped         = 4
    }

    public enum PlayerThreadState
    {
        Uninitialized   = 0,

        Prepared        = 1,

        Running         = 2,
        
        Abort           = 3
    }

    public class AudioPlayer
    {
        public AudioPlayer(string newFile)
        {
            _filePath = newFile;
            Init(_filePath);
        }

        #region Const Datas

        private const int Latency = 125;
        private int BUFFER_SIZE = 1024 * 10;
        private const int BusyQueuedBuffersThreshold = 3;

        #region File Extension
        
        private const string MP3Extension   = ".mp3";
        private const string WAVExtension   = ".wav";
        private const string OGGVExtension  = ".ogg";
        private const string FLACExtension  = ".flac";
        private const string WMAExtension   = ".wma";
        private const string AIFFExtension  = ".aiff"; 
        
        #endregion
        
        #endregion

        #region Private Members

        private string _filePath = string.Empty;

        private AudioBufferProvider provider;

        private WaveStream reader;
        private BlockAlignReductionStream blockAlignReductionStream;
        private static WaveChannel32 waveChannel;

        private static SoundTouchAPI soundTouch;

        private static IWavePlayer player;

        private Thread _playThread;

        private float _volume = 1.0f;
        private float _tempo  = 1.0f;
        private float _pitch  = 1.0f;
        private float _rate   = 1.0f;

        private bool _volumeChanged = false;
        private bool _tempoChanged  = false;
        private bool _pitchChanged  = false;
        private bool _rateChanged   = false;

        private AudioState _state = AudioState.Uninitialized;
        private PlayerThreadState _playerThreadState = PlayerThreadState.Uninitialized;

        private TimeStretchProfile _timeStretchProfile;

        private bool _stopWorker = false;

        private object PropertiesLock = new object();

        #endregion

        #region Public Properties

        public string FileName { get; private set; }

        public TimeStretchProfile TimeStretchProfile
        {
            get
            {
                lock (PropertiesLock) { return _timeStretchProfile; }
            }
            set
            {
                lock (PropertiesLock)
                {
                    _timeStretchProfile = value;
                }
            }
        }

        /// <summary>
        /// 音频音量; 
        /// 范围: 0.0 - 1.0(float); 
        /// 默认值: 1.0f
        /// </summary>
        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                _volumeChanged = true;
            }
        }

        /// <summary>
        /// 音频状态
        /// </summary>
        public AudioState State
        {
            get { return _state; }
        }

        /// <summary>
        /// 音频速度; 
        /// 范围: 0.5 - 2.0(float); 
        /// 默认值: 1.0f
        /// </summary>
        public float Tempo
        {
            get { return _tempo; }
            set
            {
                _tempo = value;
                _tempoChanged = true;
                //soundTouch.SetTempo(_tempo);
            }
        }
        
        /// <summary>
        /// 音频音调，默认值为 1.0f
        /// </summary>
        public float Pitch
        {
            get { return _pitch; }

            // TODO:暂时不对外提供
            private set
            {
                _pitch = value;
                _pitchChanged = true;
                //soundTouch.SetPitch(_pitch);
            }
        }

        /// <summary>
        /// 音频Rate
        /// </summary>
        public float Rate
        {
            get { return _rate; }
            
            // TODO:暂时不对外提供
            private set
            {
                _rate = value;
                _rateChanged = true;
                //soundTouch.SetRate(_rate);
            }
        }

        public TimeSpan CurrentPlayTime
        {
            get { return waveChannel.CurrentTime; }
            set { waveChannel.CurrentTime = value; }
        }

        public TimeSpan TotalTime
        {
            get { return waveChannel.TotalTime; }
        }

        #endregion

        #region Private Methods
        
        private bool CheckFile(string newFile)
        {
            if (!File.Exists(newFile))
            {
                return false;
            }

            FileName = Path.GetFileName(newFile);
            return true;
        }

        private void Init(string newFile)
        {
            if ( !CheckFile(newFile) )
            {
                Console.WriteLine("File is not exist!");
                return;
            }

            try
            {
                InitReader(newFile);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return;
            }

            InitSoundTouch();

            provider = new AudioBufferProvider(waveChannel.WaveFormat);

            player = new DirectSoundOut(Latency);
            player.Init(provider);
            
            _playThread = new Thread(ProcessAudio);
            _playThread.IsBackground = true;    // TODO:new 
            _playThread.Name = "PlayThread";
            _playerThreadState = PlayerThreadState.Prepared;

            _state = AudioState.Prepared;
        }
        
        private void InitReader(string newFile)
        {
            string fileExtension = Path.GetExtension(newFile);

            if (fileExtension != MP3Extension)
            {
                throw new Exception("目前仅支持Mp3文件！");
            }

            switch (fileExtension)
            {
                case MP3Extension:
                    reader = new Mp3FileReader(newFile);
                    blockAlignReductionStream = new BlockAlignReductionStream(reader);
                    waveChannel = new WaveChannel32(blockAlignReductionStream);
                    break;

                case WMAExtension:
                    break;

                case WAVExtension:
                    break;
                
                case OGGVExtension:
                    break;
                
                case FLACExtension:
                    break;
                
                case AIFFExtension:
                    break;
                
                default:
                    break;
            }
        }

        private void InitSoundTouch()
        {
            if (waveChannel == null)
            {
                throw new Exception("WaveChannel32尚未初始化！");
            }

            soundTouch = new SoundTouchAPI();
            soundTouch.CreateInstance();

            soundTouch.SetSampleRate(waveChannel.WaveFormat.SampleRate);
            soundTouch.SetChannels(waveChannel.WaveFormat.Channels);
            soundTouch.SetTempoChange(0f);
            soundTouch.SetPitchSemiTones(0f);
            soundTouch.SetRateChange(0f);
            soundTouch.SetTempo(Tempo);
            soundTouch.SetPitch(Pitch); //TODO:new
            soundTouch.SetRate(Rate);
            InitTimeStretchProfile();
        }

        private void InitTimeStretchProfile()
        {
            TimeStretchProfile = new TimeStretchProfile();
            TimeStretchProfile.AAFilterLength = 128;
            TimeStretchProfile.Description = "Optimum for Music and Speech";
            TimeStretchProfile.Id = "Practice#_Optimum";
            TimeStretchProfile.Overlap = 20;
            TimeStretchProfile.SeekWindow = 80;
            TimeStretchProfile.Sequence = 20;
            TimeStretchProfile.UseAAFilter = true;

            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_USE_AA_FILTER, TimeStretchProfile.UseAAFilter ? 1 : 0);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, TimeStretchProfile.AAFilterLength);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_OVERLAP_MS, TimeStretchProfile.Overlap);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, TimeStretchProfile.Sequence);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEEKWINDOW_MS, TimeStretchProfile.SeekWindow);
        }

        private void ApplySoundTouchTimeStretchProfile()
        {
            // "Disable" sound touch AA and revert to Automatic settings at regular tempo (to remove side effects)
            if (Math.Abs(Tempo - 1) < 0.001)
            {
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_USE_AA_FILTER, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_OVERLAP_MS, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEEKWINDOW_MS, 0);
            }
            else
            {
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_USE_AA_FILTER, _timeStretchProfile.UseAAFilter ? 1 : 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, _timeStretchProfile.AAFilterLength);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_OVERLAP_MS, _timeStretchProfile.Overlap);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, _timeStretchProfile.Sequence);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEEKWINDOW_MS, _timeStretchProfile.SeekWindow);
            }
        }

        private void ProcessAudio()
        {
            byte[] inputBuffer = new byte[BUFFER_SIZE*sizeof (float)];
            byte[] outputBuffer = new byte[BUFFER_SIZE*sizeof (float)];

            ByteAndFloatsConverter convertInputBuffer = new ByteAndFloatsConverter {Bytes = inputBuffer};
            ByteAndFloatsConverter convertOutputBuffer = new ByteAndFloatsConverter {Bytes = outputBuffer};

            int bytesRead = 0;
            _stopWorker = false;

            while (!_stopWorker && waveChannel.Position < waveChannel.Length)
            {
                SetAudioVolume();

                bytesRead = waveChannel.Read(convertInputBuffer.Bytes, 0, convertInputBuffer.Bytes.Length);
                
                SetSoundTouchValues();

                int floatsRead = bytesRead/((sizeof (float))*waveChannel.WaveFormat.Channels);
                soundTouch.PutSamples(convertInputBuffer.Floats, (uint) floatsRead);

                uint receiveCount = 0;
                do
                {// 榨干SoundTouch里所有数据
                    uint outBufferSizeFloats = (uint)convertOutputBuffer.Bytes.Length / (uint)(sizeof(float) * waveChannel.WaveFormat.Channels);
                    receiveCount = soundTouch.ReceiveSamples(convertOutputBuffer.Floats, outBufferSizeFloats);

                    if (receiveCount > 0)
                    {
                        provider.AddSamples(convertOutputBuffer.Bytes, 0, (int)receiveCount * sizeof(float)*reader.WaveFormat.Channels);

                        while (provider.BuffersCount > 3)
                        {
                            Thread.Sleep(10);
                        }
                    }

                } while (!_stopWorker && receiveCount != 0);
            }

            #region 音频自然播放结束
            
            //Pause();
            Stop();
            CurrentPlayTime = new TimeSpan(0);
            soundTouch.Clear(); 
            
            #endregion
        } 

        private void SetSoundTouchValues()
        {
            if (_tempoChanged)
            {
                float newTempo = Tempo;
                soundTouch.SetTempo(newTempo);

                _tempoChanged = false;
                ApplySoundTouchTimeStretchProfile();
            }

            if (_pitchChanged)
            {
                float newPitch = Pitch;
                soundTouch.SetPitch(newPitch);
                _pitchChanged = false;
            }

            if (_rateChanged)
            {
                float newRate = Rate;
                soundTouch.SetRate(newRate);
                _rateChanged = false;
            }
        }

        private void SetAudioVolume()
        {
            if (_volumeChanged)
            {
                float newVolume = Volume;
                waveChannel.Volume = newVolume;
                _volumeChanged = false;
            }
        }
       
        #endregion

        #region Public Methods

        public void Play()
        {
            if ( (_state != AudioState.Prepared) && (_state != AudioState.Pausing) && (_state != AudioState.Stopped) )
            {
                throw new Exception("当前状态不是 Prepared 或 Pausing 或 Stopped，不可以执行 Play");
            }

            if (_state == AudioState.Stopped)
            {
                Dispose();
                Init(_filePath);
            }

            player.Play();
            if (_playerThreadState == PlayerThreadState.Prepared)
            {
                _playThread.Start();
                _playerThreadState = PlayerThreadState.Running;
            }
            _state = AudioState.Playing;
        }

        public void Pause()
        {
            if (_state != AudioState.Playing)
            {
                throw new Exception("当前状态不是 Playing，不可执行 Pause");
            }

            player.Pause();
            _state = AudioState.Pausing;
        }
        
        public void Stop()
        {
            if ( (_state != AudioState.Playing) && (_state != AudioState.Pausing) )
            {
                throw new Exception("当前状态不是 Playing 或 Pausing，不可执行 Stop");
            }

            player.Stop();
            _state = AudioState.Stopped;
        }
        

        public void Dispose()
        {
            _playThread.Abort();
            soundTouch.Dispose();
            blockAlignReductionStream.Dispose();
            reader.Dispose();
            waveChannel.Dispose();
            player.Dispose();
        }

        #endregion

    }
}
