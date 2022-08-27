using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.CopyEngine.Core.Utils
{
    public class CEVoxelGreedyOptimizer
    {
        public struct GreedyOptimizeResult
        {
            public int x;
            public int y;
            public int sizeX;
            public int sizeY;
        }

        private static Func<int, int, int, int, bool> mIsSameTileCallback;
        private static Func<int, int, bool> mIsHaveTileCallback;

        /// <summary>
        /// 已经处理过的TileBlock标记为True
        /// </summary>
        private static Dictionary<Vector2Int, bool> mProcessAreaDic;

        private static List<GreedyOptimizeResult> mResultList;

        private static Vector2Int mBoundMin;
        private static Vector2Int mBoundMax;

        private static int mMaxSizeX;
        private static int mMaxSizeY;

        /// <summary>
        /// 优化在_boundMin和_boundMax之间的所有Tile.把相同属性的Tile进行合并
        /// 取值范围 _boundMin.xy <= $target.xy <= _boundMax.xy
        /// </summary>
        /// <param name="_boundMin">range min</param>
        /// <param name="_boundMax">range max</param>
        /// <param name="_isSameTileCallback">arg1,2 tileA.xy arg3,4 tileB.xy</param>
        /// <param name="_isHaveTileCallback">arg1,2 tile.xy</param>
        /// <returns></returns>
        public static List<GreedyOptimizeResult> Optimize(Vector2Int _boundMin, Vector2Int _boundMax,
            Func<int, int, int, int, bool> _isSameTileCallback, Func<int, int, bool> _isHaveTileCallback)
        {
            mResultList = new List<GreedyOptimizeResult>();
            mProcessAreaDic = new Dictionary<Vector2Int, bool>();
            mBoundMin = _boundMin;
            mBoundMax = _boundMax;
            mIsSameTileCallback = _isSameTileCallback;
            mIsHaveTileCallback = _isHaveTileCallback;
            for (var y = mBoundMin.y; y <= mBoundMax.y; y++)
            {
                for (var x = mBoundMin.x; x <= mBoundMax.x; x++)
                {
                    DoProcessTile(x, y);
                }
            }

            return mResultList;
        }


        private static void DoProcessTile(int _nowX, int _nowY)
        {
            if (mProcessAreaDic.ContainsKey(new Vector2Int(_nowX, _nowY)) || !mIsHaveTileCallback(_nowX, _nowY)) return; //当前位置已经被处理过了,或者当前位置没有Tile

            //Step1 取得当前行中最大的MaxX,不用关心结果 因为Step1时候自身肯定是要被Write到Result里面的,即使只有他自己一个Tile
            FindSameTileMaxX(_nowX, _nowY, _nowY, mBoundMax.x, out var nowMaxX);

            //Step2 从当前行上面一行开始去取MaxX,只要取到的值比当前值要小就返回,大或者等于就继续.
            //如果没有结果也直接返回
            var nowMaxY = _nowY; //
            for (var checkY = _nowY + 1; checkY <= mBoundMax.y; checkY++)
            {
                var isHaveResult = FindSameTileMaxX(_nowX, _nowY, checkY, nowMaxX, out var checkMaxX);
                if (!isHaveResult || checkMaxX < nowMaxX) break;
                nowMaxY = checkY; //当前行满足条件,设置NowY后继续往上去查
            }

            //写入数据
            for (var writerX = _nowX; writerX <= nowMaxX; writerX++)
            {
                for (var writerY = _nowY; writerY <= nowMaxY; writerY++)
                {
                    mProcessAreaDic.Add(new Vector2Int(writerX, writerY), true);
                }
            }

            mResultList.Add(new GreedyOptimizeResult
            {
                x = _nowX,
                y = _nowY,
                sizeX = nowMaxX - _nowX + 1,
                sizeY = nowMaxY - _nowY + 1
            });
        }

        /// <summary>
        /// 在Y=_searchY 这条线上,找寻从_startX 到 _maxSearchX 区域之间 和 Tile(_startX,_startY) 相同的Tile X最大值
        /// 如果 _searchY == _startY 则MaxX 最小为_startX 表示其自己
        /// 否则 MaxX 最小值为-1,表示不存在
        /// </summary>
        private static bool FindSameTileMaxX(int _startX, int _startY, int _staticY, int _maxSearchX, out int _maxX)
        {
            var isHaveResult = false;
            _maxX = _startX;
            for (var nowX = _startX; nowX <= _maxSearchX; nowX++)
            {
                if (mProcessAreaDic.ContainsKey(new Vector2Int(nowX, _staticY))) break; //当前位置已经被处理过
                if (!mIsHaveTileCallback(nowX, _staticY)) break; //无Tile(空洞)
                if (!mIsSameTileCallback(_startX, _startY, nowX, _staticY)) break;
                _maxX = nowX;
                isHaveResult = true; //只要设置过了一次就表示当前搜寻到数据
            }

            //如果返回了False则表示当前搜寻没有找到任何数据.
            //当在Step1时候去取MaxX,即使没有数据也是取自己,因为自己就是当前搜寻的AimTarget
            //但是对于nowY+1开始的搜寻,如果没有结果那就直接break,因为有可能表示nowY+1开始位置就没有Tile,或者开始位置的Tile就和当前目标Tile不是同一个类型的
            return isHaveResult;
        }
    }
}
