using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class RandomLibrary
    {
        private static string RandomString = "abcdefghijkmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
        private static Random random = new Random(DateTime.Now.Second);
        private static Random _random = new Random();

        #region 产生随机字符串 + GetRandomStr(int length)
        public static string GetRandomStr(int length)
        {
            string retValue = string.Empty;
            for(int i = 0; i< length; i++)
            {
                int r = _random.Next(0, RandomString.Length-1);
                retValue += RandomString[r];
            }
            return retValue;
        }
        #endregion

        #region   产生随机数 +GetRandomInt(int min,int max)
        /// <summary>
        /// 产生随机数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static int GetRandomInt(int min, int max)
        {
            return _random.Next(min,max);
        }
        #endregion
        #region 产生一个随机小数 +GetRandomDouble()
        /// <summary>
        /// 产生一个随机小数
        /// </summary>
        /// <returns></returns>
        public static double GetRandomDouble()
        {
            return _random.NextDouble();
        }
        #endregion

        #region 随机排序 +GetRandomSort<T>(T[] arr)
        /// <summary>
        /// 随机排序
        /// 因为数组是引用类型  所以不需要返回值
        /// 调用之后即改变原数组排序，直接使用即可
        /// </summary>
        /// <typeparam name="T">参数类型，必须为数组类型</typeparam>
        /// <param name="arr">数组</param>
        public static void GetRandomSort<T>(T[] arr)
        {
            int count = arr.Length;
            for (int i = 0; i < count; i++)
            {
                int rn1 = GetRandomInt(0, arr.Length);
                int rn2 = GetRandomInt(0, arr.Length);
                T temp;
                temp = arr[rn1];
                arr[rn1] = arr[rn2];
                arr[rn2] = temp;
            }
            //return arr;
        }
        #endregion
    }
}
