using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace AudioUtils.Core
{
    class AudioBufferProvider : IWaveProvider
    {
        public AudioBufferProvider(WaveFormat format)
        {
            _waveFormat = format;
            _audioBufferQueue = new Queue<AudioBuffer>();
        }

        private readonly WaveFormat _waveFormat;
        private readonly Queue<AudioBuffer> _audioBufferQueue;
        
        /// <summary>
        /// 返回当前Buffer队列数目
        /// </summary>
        public int BuffersCount { get { return _audioBufferQueue.Count; } }

        /// <summary>
        /// 音频格式
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        /// <summary>
        /// 将新的Buffer入队
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void AddSamples(byte[] buffer, int offset, int count)
        {
            byte[] nbuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, nbuffer, 0, count);
            lock (_audioBufferQueue)
            {
                _audioBufferQueue.Enqueue(new AudioBuffer(nbuffer));
            }
        }

        /// <summary>
        /// 输出设备使用Read读取Buffer来播放声音
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int required = count - read;
                AudioBuffer audioBuffer = null;
                lock (_audioBufferQueue)
                {
                    if (_audioBufferQueue.Count > 0)
                    {
                        audioBuffer = _audioBufferQueue.Peek();
                    }
                }

                if (audioBuffer == null)
                {
                    // Return a zero filled buffer
                    for (int n = 0; n < required; n++)
                        buffer[offset + n] = 0;
                    read += required;
                }
                else // There is an audio buffer - let's play it
                {
                    int nread = audioBuffer.Buffer.Length - audioBuffer.Position;

                    // If this buffer must be read in it's entirety
                    if (nread <= required)
                    {
                        // Read entire buffer
                        Buffer.BlockCopy(audioBuffer.Buffer, audioBuffer.Position, buffer, offset + read, nread);
                        read += nread;

                        lock (_audioBufferQueue)
                        {
                            _audioBufferQueue.Dequeue();
                        }
                    }
                    else // the number of bytes that can be read is greater than that required
                    {
                        Buffer.BlockCopy(audioBuffer.Buffer, audioBuffer.Position, buffer, offset + read, required);
                        audioBuffer.Position += required;
                        read += required;
                    }
                }
            }
            return read;
        }
    }

    /// <summary>
    /// 存储Buffer信息
    /// </summary>
    internal class AudioBuffer
    {
        /// <summary>
        /// 存储Buffer
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// 记录读取Buffer时的临时位置
        /// </summary>
        public int Position { get; set; }

        public AudioBuffer(byte[] newBuffer)
        {
            Buffer = newBuffer;
            Position = 0;
        }
    }
}
