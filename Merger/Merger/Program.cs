using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merger
{
    public class MessagesGenerator
    {
        public string NameConflict(string leftName, string rightName)
        {
            return string.Format("{0} <-> {1}", leftName, rightName);
        }
    }

    public class UserDialog
    {
        public UserDialog(string question, string answer)
        {
            Question = question;
            Answer = answer;
        }

        public string Question { get; private set; }
        public string Answer { get; private set; }

        internal string Ask(string question)
        {
            if (Question == question) return Answer;

            throw new Exception();
        }
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

        public string ClassFullName { get; private set; }
        public string Method { get; private set; }
        public string Variable { get; private set; }
        public string NewName { get; private set; }
    }

    public class ConflictResolver
    {
        public List<Command> ResolveConflict(UserDialog dialog, Command leftCommand, Command rightCommand)
        {
            if (leftCommand is RenameCommand && rightCommand is RenameCommand)
            {
                var leftRename = leftCommand as RenameCommand;
                var rightRename = rightCommand as RenameCommand;

                if (leftRename.ClassFullName == rightRename.ClassFullName
                    && leftRename.Method == rightRename.Method
                    && leftRename.Variable == rightRename.Variable)
                {
                    if (leftRename.NewName == rightRename.NewName)
                    {
                        return new List<Command> { leftRename };
                    }
                    else
                    {
                        var selectedName = dialog.Ask(new MessagesGenerator().NameConflict(leftRename.NewName, rightRename.NewName));
                        var result = new RenameCommand(leftRename.ClassFullName, leftRename.Method, leftRename.Variable, selectedName);
                        return new List<Command> { result };
                    }
                }
                else
                {
                    return new List<Command> { leftCommand, rightCommand };
                }
            }

            throw new Exception();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
