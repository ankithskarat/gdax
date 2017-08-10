using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gdax
{
    public class MinMax
    {
        public double min { get; set; }
        public double max { get; set; }
        public MinMax()
        {
            min = -1;
            max = -1;
        }
    }
    class Program
    {
        enum State
        {
            SELL_MODE, BUY_MODE,NO_MODE
        }
        static void Main(string[] args)
        {

            StreamReader datafile = new StreamReader(@"E:\GDAX\data1.txt\data1.txt");
            StreamWriter outfile = new StreamWriter(@"E:\GDAX\data1.txt\result_with5MinSettlement.txt");
            List<double> values = new List<double>();

            string line = "";
            while ((line = datafile.ReadLine()) != null)
            {
                values.Add(Convert.ToDouble((line.Split(',')[1].Trim())));
            }

            Dictionary<int, MinMax> minmax_at60 = new Dictionary<int, MinMax>();
            for (int start = 0; start <= values.Count()-60; start++)
            {
                minmax_at60.Add(start+59, new MinMax { min = values.GetRange(start, 60).Min(), max = values.GetRange(start,60).Max() });
            }
            Console.WriteLine("Minmax done!");
            int counter = 0;
            for (int ConsecutiveThreshold = 10; ConsecutiveThreshold > 8; ConsecutiveThreshold--)
            {
                for (double lowThreshold = 0.00; lowThreshold <= 0.99; lowThreshold += .01)
                {
                    for (double highThreshold = 0.00; highThreshold <= 0.99; highThreshold += .01)
                    {
                        counter++;
                        if (counter % 100 == 0)
                            Console.WriteLine(counter+ " iterations done. CT = "+ConsecutiveThreshold);

                        State currentState = State.SELL_MODE;
                        State orderState = State.NO_MODE;
                        double currentCoins = 30;
                        double currentCash = 0;
                        int consecutiveCheck = -1;//BUY : positive; SELL : negative;
                        int startIndex = 0, endIndex = 59;

                        int settlingtime = 300;
                        int currentsettletime = -1; //disabled timer

                        while (endIndex < values.Count())
                        {
                            //List< double > currentWindow = values.GetRange(startIndex, endIndex + 1);
                            double min = minmax_at60[endIndex].min;
                            double max = minmax_at60[endIndex].max;

                            if (currentsettletime != -1)
                                currentsettletime++;    //increment timer when buy or sell order is placed.
                            if (currentsettletime >= settlingtime && orderState == State.BUY_MODE)
                            {
                                currentCoins += currentCash / values[endIndex];
                                currentCash = 0;
                                currentsettletime = -1; //timer reset
                            }
                            if (currentsettletime >= settlingtime && orderState == State.SELL_MODE)
                            {
                                currentCash += currentCoins * values[endIndex];
                                currentCoins = 0;
                                currentsettletime = -1; //timer reset
                            }



                            if (currentState == State.BUY_MODE && values[endIndex] > min + lowThreshold)
                            {
                                if (consecutiveCheck >= ConsecutiveThreshold)
                                {
                                    currentState = State.SELL_MODE;
                                    orderState = State.BUY_MODE;
                                    if (currentsettletime == -1)
                                        currentsettletime = 0; // start timer                                    
                                }
                                else if (consecutiveCheck < 0)
                                    consecutiveCheck = 1;
                                else
                                    consecutiveCheck++;
                            }
                            else if (currentState == State.SELL_MODE && values[endIndex] < max - highThreshold)
                            {
                                if (consecutiveCheck <= ConsecutiveThreshold * -1)
                                {
                                    currentState = State.BUY_MODE;
                                    orderState = State.SELL_MODE;
                                    if (currentsettletime == -1)
                                        currentsettletime = 0; // start timer                                    
                                }
                                else if (consecutiveCheck > 0)
                                    consecutiveCheck = -1;
                                else
                                    consecutiveCheck--;
                            }
                            startIndex += 1;
                            endIndex += 1;

                            // System.out.print(output);

                        }
                        string output = highThreshold + "\t" + lowThreshold + "\t" + ConsecutiveThreshold + "\t" + currentCash + "\t" + currentCoins;
                        outfile.WriteLine(output);
                    }
                }
                Console.WriteLine("Consecutive Threshold : "+ ConsecutiveThreshold + " Done");
            }
            outfile.Close();

        }
    }
}


