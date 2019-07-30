using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class CEVoxelMeshGreedyOptimizer
    {
        public struct GreedyOptimizeResultTileInfo
        {
            public int x;
            public int y;
            public int sizeX;
            public int sizeY;
        }

        private Func<int, int, int, int, bool> mIsSameTileCallback;
        private Func<int, int, bool> mIsHaveTileCallback;

        /// <summary>
        /// 已经处理过的TileBlock标记为True
        /// </summary>
        private bool[,] mProcessedArea;

        private List<GreedyOptimizeResultTileInfo> mResultList;

        private int mMaxSizeX;
        private int mMaxSizeY;

        /// <summary>
        /// 使用Greedy算法 优化一个虚拟的二维的区域:  (0,0) - (sizeX,sizeY)
        /// *注意*
        /// - 外逻辑需要做虚拟坐标的转换,比如优化 (5,5,10) - (30,30,10) 这个二维区域 . 虚拟坐标 (2,2) 对应的就是 真实坐标 (7,7,10)
        /// - 优化的是一个平面,外逻辑需要对于一个虚拟坐标(2,2)-真实坐标(7,7,10) 其所指的是Cube的哪个面 Top?Bottom?Left? etc
        /// </summary>
        /// <param name="_sizeX">虚拟二维区域X方向大小</param>
        /// <param name="_sizeY">虚拟二维区域Y方向大小</param>
        /// <param name="_isSameTileCallback"> bool IsSameTile(int _aIndexX,int _aIndexY,int _bIndexX,int _bIndexY)</param>
        /// <param name="_isHaveTileCallback">bool IsHaveTile(int _tileX,int _tileY,)</param>
        /// <returns></returns>
        public List<GreedyOptimizeResultTileInfo> Optimize(int _sizeX, int _sizeY, Func<int, int, int, int, bool> _isSameTileCallback, Func<int, int, bool> _isHaveTileCallback)
        {
            mResultList = new List<GreedyOptimizeResultTileInfo>();
            mProcessedArea = new bool[_sizeX, _sizeY];
            mMaxSizeX = _sizeX;
            mMaxSizeY = _sizeY;
            mIsSameTileCallback = _isSameTileCallback;
            mIsHaveTileCallback = _isHaveTileCallback;
            for (var y = 0; y < _sizeY; y++)
            {
                for (var x = 0; x < _sizeX; x++)
                {
                    DoProcessTile(x, y);
                }
            }

            return mResultList;
        }


        private void DoProcessTile(int _x, int _y)
        {
            if (mProcessedArea[_x, _y]) return; //当前位置已经被处理过了

            //找寻当前行的最大值,最小值也为 _x 自身
            var maxX = FindSameTileMaxX(_x, _y, _y, mMaxSizeX - 1); // _x <= $value <= maxX || $value < 0 NotFound

            var maxY = _y; //// _y <= $valueY <= maxY

            //纵向找寻最大的Y值,因为不同行,所以最小值为 -1
            for (var nowY = _y + 1; nowY <= mMaxSizeY - 1; nowY++)
            {
                if (FindSameTileMaxX(_x, _y, nowY, maxX) < maxX) break; // _x <= $value <= maxX || $value < 0 NotFound
                maxY = nowY;
            }

            for (var findX = _x; findX <= maxX; findX++)
            {
                for (var findY = _y; findY <= maxY; findY++)
                {
                    mProcessedArea[findX, findY] = true;
                }
            }

            mResultList.Add(new GreedyOptimizeResultTileInfo
            {
                x = _x,
                y = _y,
                sizeX = maxX - _x + 1,
                sizeY = maxY - _y + 1
            });
        }

        /// <summary>
        /// 在Y=_searchY 这条线上,找寻从_startX 到 _maxSearchX 区域之间 和 Tile(_startX,_startY) 相同的Tile X最大值
        /// 如果 _searchY == _startY 则MaxX 最小为_startX 表示其自己
        /// 否则 MaxY 最小值为-1,表示不存在
        /// </summary>
        private int FindSameTileMaxX(int _startX, int _startY, int _searchY, int _maxSearchX)
        {
            var maxX = -1;
            for (var nowX = _startX; nowX <= _maxSearchX; nowX++)
            {
                if (mProcessedArea[nowX, _searchY]) break; //当前位置已经被处理过
                if (!mIsHaveTileCallback(nowX, _searchY)) break; //无Tile(空洞)
                if (!mIsSameTileCallback(_startX, _startY, nowX, _searchY)) break;
                maxX = nowX;
            }

            return maxX;
        }
    }
}