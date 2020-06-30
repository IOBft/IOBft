﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;

namespace ETModel
{
    public class CalculatePower
    {
        List<double> diffs = new List<double>();
        double difftotal = 0L;
        public int statistic = 2 * 60;

        public void Clear()
        {
            diffs.Clear();
            difftotal = 0;
        }

        public void Insert(Block blk)
        {
            if (blk == null)
                return;
            Insert(blk.GetDiff());
        }

        public double Power(double tempdiff)
        {
            string str = tempdiff.ToString("N16");
            str = str.Replace("0.", "");
            while (str.Length < 16)
            {
                str = str + "0";
            }

            double power = 1;
            for (int ii = 0; ii < str.Length; ii++)
            {
                double value1 = double.Parse("" + str[ii]);
                double value2 = value1 / (10 - value1);
                if (value2>1f)
                    power = power * value2;

                if (value1!=9)
                    break;
            }

            return power;
        }

        public void Insert(double tempdiff)
        {
            double power = Power(tempdiff);
            if (power == 0)
                return ;
            //if (difftotal != 0)
            //{
            //    power = Math.Min(difftotal * 10, power);
            //    power = Math.Max(difftotal * 0.1, power);
            //}
            diffs.Add(power);

            if (diffs.Count > statistic) // 
                diffs.RemoveAt(0);

            difftotal = 0;
            for (int i = 0; i < diffs.Count; i++)
            {
                difftotal = difftotal + diffs[i];
            }
            difftotal = difftotal / diffs.Count;
        }

        public string GetPower()
        {
            return GetPowerCompany(difftotal);
        }

        public static string GetPowerCompany(double power)
        {
            int place = 0;
            double value = power;
            while ((value / 1000) > 1)
            {
                value = value / 1000;
                place = place + 1;
            }

            String company = "";
            if (place == 1)
                company = "K";
            else
            if (place == 2)
                company = "M";
            else
            if (place == 3)
                company = "G";
            else
            if (place == 4)
                company = "T";
            else
            if (place == 5)
                company = "P";
            else
            if (place == 6)
                company = "E";

            return string.Format("{0:N2}{1}", value, company);
        }


        static public void Test()
        {
            CalculatePower calculate = new CalculatePower();

            Block blk = new Block();
            blk.prehash = "b6b67d3d8b83f4885620ccd45d1af81d5690a056de2aba8ddf899fba8088b75d";

            double diff_max = 0;
            for (int jj = 0; jj < 100; jj++)
            {
                for (int ii = 0; ii < 1000*1000; ii++)
                {
                    string random = RandomHelper.RandUInt64().ToString("x");
                    string hash = blk.ToHash(random);

                    double diff = Helper.GetDiff(hash);
                    if (diff > diff_max)
                    {
                        diff_max = diff;
                        blk.hash = hash;
                        blk.random = random;
                    }
                }

                double value1 = calculate.Power(blk.GetDiff());
                string value2 = CalculatePower.GetPowerCompany(value1);

                Log.Info( $"\n PowerCompany {blk.GetDiff()} \n {value2} \n {blk.hash}" );
            }
        }

    }

}
