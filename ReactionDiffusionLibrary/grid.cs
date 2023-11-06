using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
namespace ReactionDiffusionLibrary;

public class Grid
{
    public static int GridXSize { get; set; }
    public static int GridYSize { get; set; }
    public List<List<int[]>> Coords { get; set; } = new List<List<int[]>>();
    public List<string> Directions { get; set; } = new List<string> { "LEFT", "RIGHT", "UP", "DOWN", "STAY" };
    public List<List<List<string>>> AgentsInGrid { get; set; }
    public static string Filepath { get; set; } = $"/Users/cameronoscarmichie/Documents/GitHub/MAMOC/ReactionDiffusionLibrary/PopulationData.txt";

    public Grid(int max_x, int max_y)
    {
        GridXSize = max_x;
        GridYSize = max_y;
        AgentsInGrid = new List<List<List<string>>>();
        ClearAgentsInGrid();
    }

    public void ClearAgentsInGrid()
    {
        AgentsInGrid = new List<List<List<string>>>();
        for (int i = 0; i < GridXSize; i++)
                {
                    var innerList = new List<List<string>>();
                    for (int j = 0; j < GridYSize; j++)
                    {
                        innerList.Add(new List<string>());
                    }
                    AgentsInGrid.Add(innerList);
                }
    }

    public void Interact(Species PreySpeciesObj, Species PredSpeciesObj)
    {
        int ySize = GridYSize;
        int xSize = GridXSize;
        for (int y_i = 0; y_i < ySize; y_i++)
        {
            for (int x_i = 0; x_i < xSize; x_i++)
            {
                List<string> agents = AgentsInGrid[y_i][x_i];
                if (agents.Count == 0) continue;
                
                List<int> preys = new List<int>();
                List<int> preds = new List<int>();

                // Check each agent in a cell
                foreach (string agentStr in agents)
                {
                    if (agentStr.StartsWith('y')) preys.Add(int.Parse(agentStr.Substring(1)));
                    if (agentStr.StartsWith('d')) preds.Add(int.Parse(agentStr.Substring(1)));
                }

                // Predators breed and feed on prey
                Procreate(preds, PredSpeciesObj);
                for (int i = 0; i < preds.Count; i++)
                {
                    if (i < preys.Count)
                    {
                        Agent predator = PredSpeciesObj.AgentsList[preds[i]];
                        Agent food = PreySpeciesObj.AgentsList[preys[i]];
                        predator.Energy += food.Energy;
                        food.AddToDeathList();
                    }
                }

                // Preys breed
                Procreate(preys, PreySpeciesObj);

                // Check for special procreation cases
                if (PreySpeciesObj.Dying && preys.Count > 0) SingleProcreation(preys[0], PreySpeciesObj);
                if (PredSpeciesObj.Dying && preds.Count > 0) SingleProcreation(preds[0], PredSpeciesObj);
            }
        }

        // At the end of the turn, make babies into adults and handle deaths
        string line = $"{PreySpeciesObj.AgentsList.Count}, {PreySpeciesObj.Babies.Count-PreySpeciesObj.DeathList.Count}, {PredSpeciesObj.AgentsList.Count}, {PredSpeciesObj.Babies.Count-PredSpeciesObj.DeathList.Count}";
        Console.WriteLine(line);
        SaveLineToFile(line);
        PreySpeciesObj.NewDay();
        PredSpeciesObj.NewDay();
        ClearAgentsInGrid();
    }

    public static void Procreate(List<int> speciesList, Species speciesObj)
    {
        for (int i = 0; i < speciesList.Count - 1; i += 2)
        {
            Agent mum = speciesObj.AgentsList[speciesList[i]];
            Agent dad = speciesObj.AgentsList[speciesList[i + 1]];
            mum.Energy -= speciesList.Count;
            dad.Energy -= speciesList.Count;
            if (mum.Energy + dad.Energy > speciesObj.MinProcreationEnergy) mum.Procreate(dad);
        }
    }

    public static void InitialiseAgents(Species speciesObj)
    {
        for (int i = 0; i < speciesObj.NumAgents; i++)
        {
            Agent thisAgent = new Agent(speciesObj, i);
            speciesObj.AgentsList.Add(thisAgent);
            int agentX = (int)speciesObj.SpeciesCoords[i][0];
            int agentY = (int)speciesObj.SpeciesCoords[i][1];
            string yOrDCondition = (speciesObj.PredOrPrey == "Prey") ? "y" : "d";
            speciesObj.Grid.AgentsInGrid[agentX][agentY].Add($"{yOrDCondition}{i}");
        }
    }
    public void WriteAgentsInGridToFile()
    {
        var json = JsonSerializer.Serialize(this.AgentsInGrid);
        File.WriteAllText("/Users/cameronoscarmichie/Documents/GitHub/MAMOC/ReactionDiffusionLibrary/agentsInGrid.json", json);
    }

    public static void SingleProcreation(int agentId, Species speciesObj)
    {
        Agent mum = speciesObj.AgentsList[agentId];
        if (mum.Energy > speciesObj.MinProcreationEnergy) mum.Procreate();
    }

    public static void SaveLineToFile(string lineToSave)
    {
        try
        {
            using (StreamWriter writer = File.AppendText(Filepath)) writer.WriteLine(lineToSave);
        
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
