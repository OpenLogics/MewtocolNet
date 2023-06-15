using System;

namespace Examples;

public class ScenarioAttribute : Attribute {

    public string Description { get; private set; }    

    public ScenarioAttribute(string description) {

        Description = description;

    }

}