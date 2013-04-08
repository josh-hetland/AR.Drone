﻿using AI.Core.System;
using AR.Drone.Client.Video.Exceptions;
using FFmpeg.AutoGen;

namespace AR.Drone.Client.Video
{
    public unsafe class VideoConverter : DisposableBase
    {
        private const FFmpegNative.AVPixelFormat _inputPixelFormat = FFmpegNative.AVPixelFormat.PIX_FMT_YUV420P;

        private readonly int _height;
        private readonly int _width;
        private readonly VideoFramePixelFormat _outputPixelFormat;
        
        private byte[,,] _outputData;

        private FFmpegNative.SwsContext* _pContext;
        private FFmpegNative.AVFrame* _pCurrentFrame;


        public VideoConverter(int width, int height, VideoFramePixelFormat pixelFormat)
        {
            _width = width;
            _height = height;
            _outputPixelFormat = pixelFormat;

            Initialize();
        }

        public VideoFramePixelFormat OutputPixelFormat
        {
            get { return _outputPixelFormat; }
        }

        private void Initialize()
        {
            FFmpegNative.AVPixelFormat outputPixelFormat = VideoHelper.Convert(_outputPixelFormat);
            _pContext = FFmpegNative.sws_getContext(_width, _height, _inputPixelFormat,
                                                    _width, _height, outputPixelFormat,
                                                    FFmpegNative.SWS_FAST_BILINEAR, null, null, null);
            if (_pContext == null)
                throw new VideoConverterException("Could not initialize the conversion context.");

            _pCurrentFrame = FFmpegNative.avcodec_alloc_frame();

            int outputDataSize = FFmpegNative.avpicture_get_size(outputPixelFormat, _width, _height);
            int depth = outputDataSize/(_height*_width);
            _outputData = new byte[_height,_width,depth];

            fixed (byte* pOutputData = &_outputData[0, 0, 0])
            {
                FFmpegNative.avpicture_fill((FFmpegNative.AVPicture*) _pCurrentFrame, pOutputData, outputPixelFormat, _width, _height);
            }
        }

        public byte[,,] ConvertFrame(FFmpegNative.AVFrame frame)
        {
            fixed (byte* pOutputData = &_outputData[0, 0, 0])
            {
                byte** pSrcData = &(frame).data_0;
                byte** pDstData = &(_pCurrentFrame)->data_0;

                _pCurrentFrame->data_0 = pOutputData;
                FFmpegNative.sws_scale(_pContext, pSrcData, frame.linesize, 0, frame.height, pDstData, _pCurrentFrame->linesize);
            }
            return _outputData;
        }

        protected override void DisposeOverride()
        {
            FFmpegNative.sws_freeContext(_pContext);
            FFmpegNative.av_free(_pCurrentFrame);
        }
    }
}