using Mistreevous;

// Define some behaviour for an agent.
var definition = @"root {
    sequence {
        action [Walk]
        action [Fall]
        action [Laugh]
    }
}";

// Create an agent that we will be modelling the behaviour for.
var agent = new MyAgent();

// Create the behaviour tree, passing our tree definition and the agent.
var behaviourTree = new BehaviourTree(definition, agent);

// Step the tree.
behaviourTree.Step();