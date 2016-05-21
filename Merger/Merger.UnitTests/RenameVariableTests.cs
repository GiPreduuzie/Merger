using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Merger.UnitTests
{
    public class UserDialog : ITalkWithUser
    {
        private IEnumerable<KeyValuePair<string, string>> _replics;
        private IEnumerator<KeyValuePair<string, string>> _replicsEnumerator;

        public UserDialog(IEnumerable<KeyValuePair<string, string>> replics)
        {
            _replics = replics;
            _replicsEnumerator = _replics.GetEnumerator();
        }

        public string Ask(string question)
        {
            _replicsEnumerator.MoveNext();
            var replic = _replicsEnumerator.Current;
            Assert.AreEqual(replic.Key, question);

            return replic.Value;
        }

        internal void Completed()
        {
            Assert.IsFalse(_replicsEnumerator.MoveNext());
        }
    }

    public class Utility
    {
        internal string ApplyCommands(string code, params Command[] commands)
        {
            return commands.Aggregate(code, (accum, command) => ApplyCommand(accum, command));
        }

        internal string ApplyCommand(string code, Command command)
        {
            if (command is RenameCommand)
            {
                var renameCommand = command as RenameCommand;

                return code.Replace(renameCommand.Variable, renameCommand.NewName);
            }

            throw new Exception();
        }
    }



    public class UserQuestion
    {
        private IEnumerable<KeyValuePair<string, string>> _result;

        public UserQuestion(IEnumerable<KeyValuePair<string, string>> result)
        {
            _result = result;
        }

        public UserDialog NotAsked { get { return new UserDialog(new List<KeyValuePair<string, string>>()); } }

        public UserAnswer BeingAsked(string question)
        {
            return new UserAnswer(question, _result);
        }

        public UserDialog Done()
        {
            return new UserDialog(_result);
        }
    }

    public class UserAnswer
    {
        private IEnumerable<KeyValuePair<string, string>> _result;

        public UserAnswer(string question, IEnumerable<KeyValuePair<string, string>> result)
        {
            Question = question;
            _result = result;
        }

        public string Question { get; private set; }

        public UserQuestion Answer(string answer)
        {
            var result = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(Question, answer) };
            
            return new UserQuestion(_result.Concat(result));
        }
    }

   

    public class Commands
    {
        public Command Rename(string classFullName, string method, string variable, string newName)
        {
            return new RenameCommand(classFullName, method, variable, newName);
        }
    }

    public class CodeManager
    {
        public CodeManager(string code)
        {
            Code = code;
        }

        public string Code { get; private set; }

        internal BranchesManager ForkBranches()
        {
            return new BranchesManager(Code);
        }
    }

   

    public class BranchesManager
    {
        private string _code;
        public List<Command> _leftCommands = new List<Command>();
        public List<Command> _rightCommands = new List<Command>();

        public BranchesManager(string code)
        {
            _code = code;
        }

        public BranchesManager AddLeft(Command command)
        {
            _leftCommands.Add(command);
            return this;
        }

        public BranchesManager AddRight(Command command)
        {
            _rightCommands.Add(command);
            return this;
        }

        public CodeManager MergeBranches(UserDialog dialog)
        {
            var leftCommand = _leftCommands.Single();
            var rightCommand = _rightCommands.Single();

            var commands = new ConflictResolver().ResolveConflict(dialog, leftCommand, rightCommand);
            dialog.Completed();

            var utility = new Utility();

            return new CodeManager(utility.ApplyCommands(_code, commands.ToArray()));
        }
    }

    public class Library
    {
        internal CodeManager GetCode(string fileName)
        {
            return new CodeManager(File.ReadAllText(@"C:\Dev\Merger\Library\" + fileName + ".cs"));
        }
    }

    public class TestBase
    {
        public Library Library => new Library();
        public Commands Commands { get { return new Commands(); } }
        public UserQuestion User { get { return new UserQuestion(new List<KeyValuePair<string, string>>()); } }
        public MessagesGenerator MessagesGenerator { get { return new MessagesGenerator(); } }
        public Utility Utility { get { return new Utility(); } }
    }

    [TestClass]
    public class GeneralCases : TestBase
    {
        [TestMethod]
        public void RenameVariable_Confilct()
        {
            var startCode = Library.GetCode("Ex_1");

            var resultCode =

            startCode
                .ForkBranches()
                .AddLeft(Commands.Rename("GeneralCase", "Method", "variable", "variable1"))
                .AddRight(Commands.Rename("GeneralCase", "Method", "variable", "variable2"))
                .MergeBranches(
                    User
                        .BeingAsked(MessagesGenerator.VariableHasBeenRenamedDifferently("variable1", "variable2"))
                        .Answer("variable3")
                        .Done())
                .Code;

            Assert.AreEqual(Utility.ApplyCommand(startCode.Code, Commands.Rename("GeneralCase", "Method", "variable", "variable3")), resultCode);
        }

        [TestMethod]
        public void Rename2Variables_NoConfilct()
        {
            var startCode = Library.GetCode("Ex_1");

            var resultCode =

            startCode
                .ForkBranches()
                .AddLeft(Commands.Rename("GeneralCase", "Method", "variable", "variable1"))
                .AddRight(Commands.Rename("GeneralCase", "Method", "tail", "tail1"))
                .MergeBranches(User.NotAsked)
                .Code;

            Assert.AreEqual(
                
                Utility.ApplyCommands(
                    startCode.Code,
                    Commands.Rename("GeneralCase", "Method", "variable", "variable1"),
                    Commands.Rename("GeneralCase", "Method", "tail", "tail1")),
                resultCode);
        }

        [TestMethod]
        public void Rename2Variables_Confilct()
        {
            var startCode = Library.GetCode("Ex_1");

            var resultCode =

            startCode
                .ForkBranches()
                .AddLeft(Commands.Rename("GeneralCase", "Method", "variable", "variable1"))
                .AddRight(Commands.Rename("GeneralCase", "Method", "tail", "variable1"))
                .MergeBranches(
                   User
                       .BeingAsked(MessagesGenerator.VariablesHaveBeenRenamedWithConflict("variable", "variable1"))
                       .Answer("variable1")
                       .BeingAsked(MessagesGenerator.VariablesHaveBeenRenamedWithConflict("tail", "variable1"))
                       .Answer("tail2")
                       .Done())
                .Code;

            Assert.AreEqual(
                Utility.ApplyCommands(
                    startCode.Code,
                    Commands.Rename("GeneralCase", "Method", "variable", "variable1"),
                    Commands.Rename("GeneralCase", "Method", "tail", "tail2")),
                resultCode);
        }
    }
}
