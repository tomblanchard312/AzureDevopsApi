namespace ADOApi.Models { 

public class WorkItemCreateModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string AssignedTo { get; set; }
    public string AreaPath { get; set; }
    public string IterationPath { get; set; }
    public string State { get; set; }
    public string Reason { get; set; }
    public int Priority { get; set; }
    public string ReproSteps { get; set; } // Common in Bug work items
    public string Severity { get; set; } // Common in Bug work items
    public List<string> Tags { get; set; }
    // Custom fields can be added as needed. Example:
    // public string CustomField { get; set; }

    // Add other properties as necessary based on your Azure DevOps setup
}
}