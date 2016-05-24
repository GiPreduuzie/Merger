using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merger
{
    public class MessagesGenerator
    {
        public string VariableHasBeenRenamedDifferently(string leftName, string rightName)
        {
            return $"{leftName} <-> {rightName}";
        }

        public string VariablesHaveBeenRenamedWithConflict(string oldName, string newName)
        {
            return $"{oldName} <-> {newName}";
        }
    }

    public interface ITalkWithUser
    {
        string Ask(string question);
    }


    public class Command
    {

    }

    public class RenameCommand : Command
    {
        public RenameCommand(string classFullName, string method, string variable, string newName)
        {
            ClassFullName = classFullName;
            Method = method;
            Variable = variable;
            NewName = newName;
        }

        public string ClassFullName { get; }
        public string Method { get; }
        public string Variable { get; }
        public string NewName { get; }
    }

    public class MoveMethodCommand : Command
    {
        public string ClassFullName { get; }
        public string Method { get; }
        public bool MoveUp { get;  }

        public MoveMethodCommand(string classFullName, string method, bool moveUp)
        {
            ClassFullName = classFullName;
            Method = method;
            MoveUp = moveUp;
        }
    }

    public class ConflictResolver
    {
        public List<Command> ResolveConflict(ITalkWithUser dialog, Command leftCommand, Command rightCommand)
        {
            if (leftCommand is RenameCommand && rightCommand is RenameCommand)
            {
                return Process(dialog, leftCommand as RenameCommand, rightCommand as RenameCommand);
            }
            else
            {
                if (leftCommand is RenameCommand && rightCommand is MoveMethodCommand)
                {
                    return Process(dialog, leftCommand as RenameCommand, rightCommand as MoveMethodCommand);
                }
                else
                {
                    if (leftCommand is MoveMethodCommand && rightCommand is RenameCommand)
                        return Process(dialog, rightCommand as RenameCommand, leftCommand as MoveMethodCommand);
                }


                throw new Exception();
            }
        }

        private static List<Command> Process
            (ITalkWithUser dialog,
            RenameCommand leftCommand,
            MoveMethodCommand rightCommand)
        {
            return new List<Command>() {leftCommand, rightCommand};
        }

        private static List<Command> Process(
            ITalkWithUser dialog,
            RenameCommand leftCommand,
            RenameCommand rightCommand)
        {
            if (leftCommand.ClassFullName == rightCommand.ClassFullName
                && leftCommand.Method == rightCommand.Method)
            {
                if (leftCommand.Variable == rightCommand.Variable)
                {
                    if (leftCommand.NewName == rightCommand.NewName)
                    {
                        return new List<Command> {leftCommand};
                    }
                    else
                    {
                        var selectedName =
                            dialog.Ask(new MessagesGenerator().VariableHasBeenRenamedDifferently(leftCommand.NewName,
                                rightCommand.NewName));
                        var result = new RenameCommand(leftCommand.ClassFullName, leftCommand.Method, leftCommand.Variable,
                            selectedName);
                        return new List<Command> {result};
                    }
                }
                else
                {
                    if (leftCommand.NewName == rightCommand.NewName)
                    {
                        var newLeftName =
                            dialog.Ask(new MessagesGenerator().VariablesHaveBeenRenamedWithConflict(leftCommand.Variable,
                                leftCommand.NewName));
                        var newRightName =
                            dialog.Ask(new MessagesGenerator().VariablesHaveBeenRenamedWithConflict(rightCommand.Variable,
                                rightCommand.NewName));

                        if (newLeftName == newRightName) throw new Exception();

                        return new List<Command>
                        {
                            new RenameCommand(leftCommand.ClassFullName, leftCommand.Method, leftCommand.Variable, newLeftName),
                            new RenameCommand(rightCommand.ClassFullName, rightCommand.Method, rightCommand.Variable, newRightName)
                        };
                    }
                    else
                    {
                        return new List<Command> { leftCommand, rightCommand };
                    }
                }
            }
            else
            {
                return new List<Command> { leftCommand, rightCommand };
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
