using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NAudio.Wave;

namespace AudioUtils.Core
{
    /// <summary>
    /// Audio States
    /// 音频播放状态
    /// </summary>
    public enum AudioState
    {
        Uninitialized   = 0,

        Prepared        = 1,
        
        Playing         = 2,
        
        Pausing         = 3,
        
        Stopped         = 4
    }


    /// <summary>
    /// 播放线程状态
    /// </summary>
    public enum PlayerThreadState
    {
        Uninitialized   = 0,

        Prepared        = 1,

        Running         = 2,
        
        Abort           = 3
    }


    class AudioPlayerCore
    {

        #region Const Datas

        private const int Latency = 125;
        private const int BUFFER_SIZE = 1024 * 10;
        private const int BusyQueuedBuffersThreshold = 3;

        #region File Extension

        private const string MP3Extension = ".mp3";
        private const string WAVExtension = ".wav";
        private const string OGGVExtension = ".ogg";
        private const string FLACExtension = ".flac";
        private const string WMAExtension = ".wma";
        private const string AIFFExtension = ".aiff";

        #endregion

        #endregion


        #region Private Members



        #endregion


        #region Private Methods

        private string _filePath = string.Empty;

        private WaveStream _reader;
        private BlockAlignReductionStream _blockAlignReductionStream;
        private WaveChannel32 _waveChannel32;

        private SoundTouch.SoundTouchAPI _soundTouch;
        
        private AudioBufferProvider _provider;

        private IWavePlayer _player;

        private AudioState _audioState = AudioState.Uninitialized;
        private PlayerThreadState _playerThreadState = PlayerThreadState.Uninitialized;

        private Thread _playThread;

        private TimeStretchProfile _timeStretchProfile;

        private bool _stopWorker = false;

        private float _volume = 1.0f;
        private float _tempo  = 1.0f;
        private float _pitch  = 1.0f;
        private float _rate   = 1.0f;

        private bool _volumeChanged = false;
        private bool _tempoChanged  = false;
        private bool _pitchChanged  = false;
        private bool _rateChanged   = false;

        private object PropertiesLock = new object();

        #endregion


        #region Public Properties

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public AudioState State
        {
            get { return _audioState; }
        }

        /// <summary>
        /// 音频格式信息
        /// </summary>
        public WaveFormat Format
        {
            get { return _reader.WaveFormat; }
        }

        /// <summary>
        /// 音频经SoundTouch处理时的信息
        /// </summary>
        public TimeStretchProfile TimeStretchProfile
        {
            get
            {
                lock (PropertiesLock)
                {
                    return _timeStretchProfile;
                }
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
        /// 范围: 0.0f - 1.0f;
        /// 默认: 1.0f
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
        /// 音频速度;
        /// 建议范围: 0.5f - 2.0f;
        /// 默认: 1.0f
        /// </summary>
        public float Tempo
        {
            get { return _tempo; }
            set
            {
                _tempo = value;
                _tempoChanged = true;
            }
        }

        /// <summary>
        /// 音频音调;
        /// 范围: x.xf - x.xf;
        /// 默认: 1.0f
        /// TODO: 暂不对外提供
        /// </summary>
        public float Pitch
        {
            get { return _pitch; }
            set
            {
                _pitch = value;
                _pitchChanged = true;
            }
        }

        /// <summary>
        /// 音频Rate;
        /// 范围: x.xf - x.xf;
        /// 默认: 1.0f
        /// TODO: 暂不对外提供
        /// </summary>
        public float Rate
        {
            get { return _rate; }
            set
            {
                _rate = value;
                _rateChanged = true;
            }
        }

        /// <summary>
        /// 当前播放时间
        /// </summary>
        public TimeSpan CurrentPlayTime
        {
            get { return _waveChannel32.CurrentTime; }
            set { _waveChannel32.CurrentTime = value; }
        }

        /// <summary>
        /// 音频总时间
        /// </summary>
        public TimeSpan TotalTime
        {
            get { return _waveChannel32.TotalTime; }
        }

        #endregion


        #region Public Mehtods

        #endregion


        #region Public Events

        #endregion
    }
}
