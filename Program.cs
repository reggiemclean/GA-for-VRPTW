using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Security.Cryptography;

namespace Assignment1 {

    class NumWithCount
    {
        public int Element { get; set; }
        public int Count { get; set; }
    }

    class CustomerInfo
    {
        public int CustomerNumber { get; set; }
        public int XCoordinate { get; set; }
        public int YCoordinate { get; set; }
        public int Demand { get; set; }
        public int ReadyTime { get; set; }
        public int DueDate { get; set; }
        public int ServiceTime { get; set; }
    }

    class ParetoRank
    {
        public double distance { get; set; }
        public int trucks { get; set; }
        public int element { get; set; }
    }

    
    class Program {
        //*******************************
        const int GA_POPSIZE = 5;
        const double GA_ELITISMRATE = 0.1;
        const double GA_MUTATIONRATE = 0.2;
        const double GA_CROSSOVERRATE = 0.9;
        const int num_customers = 100;
        const int MAX_CAPACITY = 100;
        //*******************************

        //**************************************************************************
        public static List<CustomerInfo> customerList = new List<CustomerInfo>();
        public static double [] fitnessPopulation = new double[GA_POPSIZE];
        public static int[] NumberOfTrucks = new int[GA_POPSIZE];
        public static int[,] PotentialParents = new int[num_customers, GA_POPSIZE];
        public static int[,] NextPopulation = new int[num_customers, GA_POPSIZE];
        public static double[] fitnessNextPopulation = new double[GA_POPSIZE];
        public static double GlobalBestFitness = 999999999; // the global variable to keep track of the global best distance that has been found
        public static int BestSolutionIndex = 0;
        public static List<ParetoRank> mutationList = new List<ParetoRank>();
        //**************************************************************************

        static void Main(string[] args)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int[,] ga_population = new int[num_customers, GA_POPSIZE];
            ga_population = InitPop(GA_POPSIZE);
            ReadFile();
            fitnessPopulation = CalculateFitness(ga_population);
            ParetoRank(ga_population, fitnessPopulation, NumberOfTrucks);
            //fitnessPopulation = CalculateFitnessWeightedSum(ga_population, fitnessPopulation);
            for (int gen = 1 ; gen <= 250; gen++)
			{
                TournamentSelection(2, ga_population);
                OnePointCrossover(rand.Next(0,num_customers));
                Mutation();
                Elitism(ga_population);
                fitnessNextPopulation = CalculateFitness(NextPopulation);
                Array.Copy(NextPopulation, ga_population, NextPopulation.Length);

                //NextPopulation = CheckRoute(NextPopulation);
                //UniformOrderCrossover(ga_population);
                //CycleCrossover();
                //fitnessNextPopulation = CalculateFitnessWeightedSum(NextPopulation, fitnessNextPopulation);
            }
            Console.WriteLine("Lowest Distance found: " + fitnessNextPopulation.Min());
            for (int x = 0; x < GA_POPSIZE; x++)
                if (fitnessNextPopulation[x] == fitnessNextPopulation.Min())
                {
                    Console.WriteLine("This is route #: " + x);
                    BestSolutionIndex = x;
                    PrintRoute(x, ga_population);
                    Console.WriteLine();
                }
            //WriteFile(fitnessNextPopulation, NumberOfTrucks);
         Console.ReadKey();
		}

		static int[,] InitPop(int pop_size)
		{
            int[,] init_population = new int[num_customers, pop_size];
            List<int> RandomNumbers = new List<int>();
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            int customer = rnd.Next(1, num_customers + 1);
            for (int x = 0; x < pop_size ; x++)
            {
                for (int y= 0; y < num_customers; y++)
                {
                    if (y == 0)
                        customer = 0;
                    while (RandomNumbers.Contains(customer))
                    {
                        customer = rnd.Next(0, num_customers);
                    }
                    init_population[y, x] = customer;
                    
                    RandomNumbers.Add(customer);
                }
                RandomNumbers.Clear();
            }
			return init_population;
		}

        static void UniformOrderCrossover (int [,] pop)
        {
            Random rand = new Random();
            List<int> childAMultiple = new List<int>();
            List<int> childBMultiple = new List<int>();
            for (int x = 0; x < GA_POPSIZE-1; x+=2)
            {
                for (int y = 0; y < num_customers; y++)
                {
                    if (rand.NextDouble() < GA_CROSSOVERRATE) // if crossover is to happen
                    {
                        if (rand.NextDouble() < 0.5)
                        {
                            //swap bits for next gen  
                            NextPopulation[y, x] = PotentialParents[y, x + 1];
                            NextPopulation[y, x + 1] = PotentialParents[y, x];
                        }
                        else
                        {
                            // keep bit same in next gen
                            NextPopulation[y, x] = PotentialParents[y, x];
                            NextPopulation[y, x + 1] = PotentialParents[y, x + 1];
                        }
                    }
                    else
                    {
                        // keep bit same in next gen
                        NextPopulation[y, x] = PotentialParents[y, x];
                        NextPopulation[y, x + 1] = PotentialParents[y, x + 1];
                    }
                }
            }
        }

        static void CycleCrossover()
        {
            Random rand = new Random();
            int numCycles = rand.Next(0, 20);
            int indexForCross = 0;
            List<int> cyclePoints = new List<int>();
            int count = 0;
            while (cyclePoints.Count < numCycles)
            {   
                while (cyclePoints.Contains(indexForCross))
                {
                    indexForCross = rand.Next(0, num_customers);
                }
                cyclePoints.Add(indexForCross);
            }
            cyclePoints.Sort();

            for (int x = 0; x < GA_POPSIZE-1; x+=2)
            {
                for (int y = 0; y < num_customers; y++)
                {
                    if (rand.NextDouble() < GA_CROSSOVERRATE)
                    {

                        if (y == cyclePoints[count] && cyclePoints.Count > 0)
                        {
                            //don't cross
                            cyclePoints.RemoveAt(0);
                            NextPopulation[y, x] = PotentialParents[y, x];
                            NextPopulation[y, x + 1] = PotentialParents[y, x + 1];
                            count++;
                        }
                        else
                        {
                            NextPopulation[y, x] = PotentialParents[y, x + 1];
                            NextPopulation[y, x + 1] = PotentialParents[y, x];
                            //cross 
                        }
                    }
                    else
                    {
                        NextPopulation[y, x] = PotentialParents[y, x];
                        NextPopulation[y, x + 1] = PotentialParents[y, x + 1];
                    }
                }
            }
        }

        static void OnePointCrossover (int IndexForCross)
		{
            int[] ChildA = new int[num_customers];
            int[] ChildB = new int[num_customers];
            List<int> childA = new List<int>();
            List<int> childB = new List<int>();
            Random rand = new Random(Guid.NewGuid().GetHashCode());

            
            //*******************************************************************
            for (int x = 0; x <= GA_POPSIZE - 1; x+=2)
            {
                for (int y = 0; y < num_customers; y++)
                {
                    if (rand.NextDouble() < GA_CROSSOVERRATE)
                    {
                    
                        if (y < IndexForCross) // the point of crossover still hasn't come up
                        {
                            ChildA[y] = PotentialParents[y, x];
                            ChildB[y] = PotentialParents[y, x+1];
                        }
                        else //at or past the crossover point
                        {
                            ChildA[y] = PotentialParents[y, x + 1];
                            ChildB[y] = PotentialParents[y, x];
                        }
                    }
                    else
                    {
                        ChildA[y] = PotentialParents[y, x];
                        ChildB[y] = PotentialParents[y, x + 1];
                    }

                }
                for (int b = 0; b < num_customers; b++)
                {
                    NextPopulation[b, x] = ChildA[b];
                    NextPopulation[b, x + 1] = ChildB[b];
                }
            }
        }  

        static double[] CalculateFitness(int[,] pop) // need to insert the depot before and after each route
        {
            double[] fitnessPopulation = new double[GA_POPSIZE];
            //Single Objective - Evaluating the distance travelled
            double CalcFitness = 0;
            int capacity = 0;
            int numTrucks = 1;
            int FirstCity = 0;
            List<int> duplicates = new List<int>();
            for (int x = 0; x < GA_POPSIZE; x++)
            {
                capacity = customerList[pop[0, x]].Demand;
                for (int y = 0; y < num_customers - 1; y++)
                {
                    
                    //serviceTime = customerList[pop[y, x]].ServiceTime;
                    if (capacity + customerList[pop[y + 1, x]].Demand <= MAX_CAPACITY)
                    {
                        CalcFitness += Math.Sqrt(Math.Pow(Math.Abs(customerList[pop[y, x]].XCoordinate - customerList[pop[y + 1, x]].XCoordinate), 2) + Math.Pow(Math.Abs(customerList[pop[y, x]].YCoordinate - customerList[pop[y + 1, x]].YCoordinate), 2));
                        capacity += customerList[pop[y + 1, x]].Demand;
                    }
                    else
                    {
                        CalcFitness += Math.Sqrt(Math.Pow(Math.Abs(customerList[pop[y, x]].XCoordinate - customerList[0].XCoordinate), 2) + Math.Pow(Math.Abs(customerList[pop[y, x]].YCoordinate - customerList[0].YCoordinate), 2));
                        // link last truck to go back to start
                        capacity = customerList[pop[y + 1, x]].Demand; // next customer cannot be added to this truck, add a new truck and reset capacity to new customer
                        numTrucks++;
                    }
                }
                mutationList.Add(new ParetoRank { element = x, distance = CalcFitness, trucks = numTrucks });
                fitnessPopulation[x] = CalcFitness;
                int check = duplicates.Distinct().Count();

                if (CalcFitness < GlobalBestFitness)
                {
                    GlobalBestFitness = CalcFitness;
                    BestSolutionIndex = x;
                }

                NumberOfTrucks[x] = numTrucks;
                numTrucks = 1;
                CalcFitness = 0;

            }
            return fitnessPopulation;
        }

        static double[] CalculateFitnessWeightedSum(int[,] pop, double[] fitnessPopulation)
        {
            //Single Objective - Evaluating the distance travelled
            double CalcFitness = 0;
            int capacity = 0;
            int numTrucks = 1;
            int FirstCity = 0;
            List<int> duplicates = new List<int>();
            for (int x = 0; x < GA_POPSIZE; x++)
            {
                capacity = customerList[pop[0, x]].Demand;
                for (int y = 0; y < num_customers - 1; y++)
                {
                    duplicates.Add(pop[y, x]);
                    if (capacity + customerList[pop[y + 1, x]].Demand <= MAX_CAPACITY)
                    {
                        CalcFitness += Math.Sqrt(Math.Pow(Math.Abs(customerList[pop[y, x]].XCoordinate - customerList[pop[y + 1, x]].XCoordinate), 2) + Math.Pow(Math.Abs(customerList[pop[y, x]].YCoordinate - customerList[pop[y + 1, x]].YCoordinate), 2));
                        capacity += customerList[pop[y + 1, x]].Demand;
                    }
                    else
                    {
                        CalcFitness += Math.Sqrt(Math.Pow(Math.Abs(customerList[pop[y, x]].XCoordinate - customerList[pop[FirstCity, x]].XCoordinate), 2) + Math.Pow(Math.Abs(customerList[pop[y, x]].YCoordinate - customerList[pop[FirstCity, x]].YCoordinate), 2));
                        // link last truck to go back to start
                        FirstCity = y + 1;
                        capacity = customerList[pop[y + 1, x]].Demand; // next customer cannot be added to this truck, add a new truck and reset capacity to new customer
                        numTrucks++;
                    }
                }

                fitnessPopulation[x] = CalcFitness*0.8 + 0.7*numTrucks;
                //int check = duplicates.Distinct().Count();

                if (CalcFitness < GlobalBestFitness)
                {
                    GlobalBestFitness = CalcFitness;
                    BestSolutionIndex = x;
                }

                NumberOfTrucks[x] = numTrucks;
                numTrucks = 1;
                CalcFitness = 0;

            }
            return fitnessPopulation;
        }

        static int[] ParetoRank(int[,] pop, double [] distance, int[] numberOfTrucks)
        {
            int count = 0;
            int currentRank = 1;
            int pop_size = GA_POPSIZE;
            int N = pop_size;
            int[] rank = new int[GA_POPSIZE];
            List<ParetoRank> sorted = new List<ParetoRank>();
            List<ParetoRank> chromosomes = new List<ParetoRank>();
            for(int p = 0; p < GA_POPSIZE; p++)
            {
                Console.WriteLine("Distance: " + distance[p] + " Trucks: " + numberOfTrucks[p]);
            }
            for (int x = 0; x< GA_POPSIZE; x++)
            {
                chromosomes.Add(new ParetoRank { distance = distance[x], trucks = numberOfTrucks[x], element = x});
            }
            sorted = chromosomes.OrderBy(x => x.distance).ToList();
            while (N >= 1)
            {
                for (int i =0; i<N; i++)
                {
                    if (Dominated(sorted[i], sorted, i)==true) // just need to look at how the distance and the number of trucks ranks
                    {
                        rank[i] = currentRank;
                    }
                }
                for (int j = 0; j<N; j++)
                {
                    if (rank[j] == currentRank)
                    {
                        //remove pop at j from population
                        sorted.RemoveAt(j);
                        //pop_size--;
                    }
                }
                currentRank++;
                N = sorted.Count;
            }
            return rank;
        }

        static bool Dominated (ParetoRank check, List<ParetoRank> set, int checkIndex)
        {
            List<bool> value = new List<bool>();
            int x = checkIndex;
            //going to return true if this element is non dominated
            while(x<set.Count)
            {
                if (check.distance > set[x].distance && check.trucks >= set[x].trucks)
                {
                    value.Add(false);
                }
                else if (check.distance > set[x].distance || check.trucks >= set[x].trucks)
                    value.Add(true);
                x++;
            }

            if (value.Contains(false))
                return false;
            else
                return true;
        }

        static void TournamentSelection(int k, int [,] population) 
        {
            int BestParentFound = 0;
            double BestLocalFitness = 9999999999;
            Random rand = new Random(Guid.NewGuid().GetHashCode()); // trying to generate "better" random numbers
            int PopCount = 0;
            int ParentCount = 0;
            List<int> ParentLocations = new List<int>();
            while (PopCount < GA_POPSIZE)
            {
                while (ParentCount < k) //adds k parents to a list of potential parents
                {
                    ParentLocations.Add(rand.Next(0, GA_POPSIZE));
                    ParentCount++;
                }

                foreach (int parent in ParentLocations) // for each of the parents found and placed in list, checks if that parent is the best found parent
                {
                    if (fitnessPopulation[parent] < BestLocalFitness)
                    {
                        BestLocalFitness = fitnessPopulation[parent];
                        BestParentFound = parent;
                    }
                }
                //add best parent to PotentialParents
                for (int x = 0; x < num_customers; x++)
                    PotentialParents[x, PopCount] = population[x, BestParentFound];
                if (BestLocalFitness < GlobalBestFitness)
                    GlobalBestFitness = BestLocalFitness;
                PopCount++;
                ParentCount = 0;
                BestLocalFitness = 999999999999;
                ParentLocations.Clear();
            }
        }

        static void Elitism(int [,] pop)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            double rate = rand.NextDouble();
            int numToInsert = rand.Next(10);
            List<ParetoRank> sortedMin = mutationList.OrderBy(x => x.distance).ToList();
            List<ParetoRank> sortedMax = mutationList.OrderByDescending(x => x.distance).ToList();
            while (numToInsert > 0)
            {
                if (rate < GA_ELITISMRATE)
                { 
                    for (int x = 0; x < num_customers; x++)
                    {
                        NextPopulation[x, sortedMax.First().element] = pop[x,sortedMin.First().element];
                        
                    }
                    sortedMax.RemoveAt(0);
                    sortedMin.RemoveAt(0);
                    
                }
                rate = rand.NextDouble();
                Thread.Sleep(1);
                numToInsert--;
            }
        }

        static void Mutation ()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            List<int> toBeSwapped = new List<int>();
            int pointA, pointB;
            for (int x = 0; x < GA_POPSIZE; x++)
            {
                if (rand.NextDouble() > 0.9) // mutation should happen if value is between 0.9 and 1
                {
                    pointA = rand.Next(num_customers);
                    //Thread.Sleep(10);
                    pointB = rand.Next(num_customers-pointA);
                    for (int y = 0; y < num_customers; y++)
                    {
                        //inversion mutation = two points picked, chromosome is reversed within that range.
                            toBeSwapped.Add(NextPopulation[y, x]);
                    }
                    toBeSwapped.Reverse(pointA, pointB);
                    for (int g = 0; g <num_customers; g++)
                    {
                        NextPopulation[g, x] = toBeSwapped[g];
                    }
                }
            }
        }

        static void ReadFile()
        {
            var lines = System.IO.File.ReadAllLines("C:\\Users\\Reggie\\Desktop\\data\\R101_200.csv");
            foreach (string customer in lines)
            {
                var values = customer.Split(',');
                customerList.Add(new CustomerInfo()
                {
                    CustomerNumber = Convert.ToInt32(values[0]),
                    XCoordinate = Convert.ToInt32(values[1]),
                    YCoordinate = Convert.ToInt32(values[2]),
                    Demand = Convert.ToInt32(values[3]),
                    ReadyTime = Convert.ToInt32(values[4]),
                    DueDate = Convert.ToInt32(values[5]),
                    ServiceTime = Convert.ToInt32(values[6])
                });
            }
        }

        static void WriteFile (double [] distance, int[] trucks)
        {
            using (StreamWriter outfile = new StreamWriter(@"C:\\Users\\Reggie\\Desktop\\distance.csv"))
            {
                for (int x = 0; x < GA_POPSIZE; x++)
                {
                    string content = "";
                    content += distance[x].ToString("0.00");
                    outfile.WriteLine(content);
                }
            }
            
            using (StreamWriter outfi = new StreamWriter(@"C:\\Users\\Reggie\Desktop\\trucks.csv"))
            {
                for(int y =0; y < GA_POPSIZE; y++)
                {
                    string content = "";
                    content += trucks[y].ToString();
                    outfi.WriteLine(content);
                }
            }
        }

        static void PrintRoute (int index, int [,] pop)
        {
            int trucks = 1;
            int currentCapacity = 0;
            int count = 0;
           
            while (count < num_customers)
            {
                Console.WriteLine("Truck #: " + trucks + "'s Route is: ");
                Console.Write("0 ");
                while (count < num_customers  && currentCapacity + customerList[pop[count, BestSolutionIndex]].Demand <= MAX_CAPACITY)
                {
                    currentCapacity += customerList[ pop[count, BestSolutionIndex] ].Demand;
                    Console.Write(pop[count, BestSolutionIndex] + " ");
                    count++;
                }
                Console.Write("0");
                Console.WriteLine();
                currentCapacity = 0;
                trucks++;
            }
        }

        static int[,] CheckRoute (int [,] pop)
        {
            List<NumWithCount> check = new List<NumWithCount>();
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            List<NumWithCount> shuffle = new List<NumWithCount>();
            int count = 0;
            int control = 0;
            bool findValues = false;
            while (count < GA_POPSIZE)
            {
                for (int x = 0; x < num_customers; x++)
                {
                    check.Add(new NumWithCount { Count = 0, Element = x });
                }
                for (int y = 0; y < num_customers; y++)
                {
                    check[pop[y, count]].Count++;
                }

                shuffle = check.OrderBy(x => GetNextInt32(rnd)).ToList();

                for (int u = 0; u < num_customers; u++)
                {
                    //Console.WriteLine("Check: " + pop[u, count] + " " + " for customer: " + u + " Pop #: " + count + " Check: " + check[pop[u, count]].Element);
                    while (shuffle[pop[u, count]].Count > 1)
                    {
                        control = 0;
                        findValues = false;
                        while (findValues == false && control < num_customers)
                        {
                            if (shuffle[control].Count == 0)
                            {
                                shuffle[pop[u, count]].Count--;
                                pop[u, count] = shuffle[control].Element;
                                shuffle[control].Count++;
                                findValues = true;
                                control = num_customers;
                            }
                            control++;
                        }
                    }
                }
                shuffle.Clear();
                check.Clear();
                count++;
            }
            return pop; 
        }

        static int GetNextInt32(RNGCryptoServiceProvider rnd)
        {
            byte[] randomInt = new byte[4];
            rnd.GetBytes(randomInt);
            return Convert.ToInt32(randomInt[0]);
        }
    }
}