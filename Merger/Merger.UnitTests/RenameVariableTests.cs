using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Merger.UnitTests
{
    public class UserDialog : ITalkWithUser
    {
        private readonly IEnumerable<KeyValuePair<string, string>> _replics;
        private readonly IEnumerator<KeyValuePair<string, string>> _replicsEnumerator;

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
            return commands.Aggregate(code, ApplyCommand);
        }

        internal string ApplyCommand(string code, Command command)
        {
            if (command is RenameCommand)
            {
                var renameCommand = command as RenameCommand;

                return code.Replace(renameCommand.Variable, renameCommand.NewName);
            }

            if (command is MoveMethodCommand)
            {
                var lines = code.Replace("\r\n", "\n").Split('\n').ToArray();

                var head = lines.Take(4);
                var tail = new List<string> { lines.Last()};

                var method1 = lines.Skip(4).Take(5).ToArray();
                var emptyLine = new List<string> { lines[9]};
                var method2 = lines.Skip(10).Take(5).ToArray();

                var name1 = method1[0].Split(' ', '(', ')').Where(x => x != "").ElementAt(2);
                var name2 = method2[0].Split(' ', '(', ')').Where(x => x != "").ElementAt(2);

                var moveMethodCommand = command as MoveMethodCommand;

                var ordered1 = method1;
                var ordered2 = method2;

                if (moveMethodCommand.Method == name2)
                {
                    ordered1 = method2;
                    ordered2 = method1;
                }

                var result = string.Join(Environment.NewLine, head.Concat(ordered1).Concat(emptyLine).Concat(ordered2).Concat(tail));
                return result;
            }

            throw new Exception();
        }
    }



    public class UserQuestion
    {
        private readonly IEnumerable<KeyValuePair<string, string>> _result;

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
        private readonly IEnumerable<KeyValuePair<string, string>> _result;

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

        public Command MoveMethodUp(string classFullName, string methodb)
        {
            return new MoveMethodCommand(classFullName, methodb, true);
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
        private readonly string _code;
        public List<Command> LeftCommands = new List<Command>();
        public List<Command> RightCommands = new List<Command>();

        public BranchesManager(string code)
        {
            _code = code;
        }

        public BranchesManager AddLeft(Command command)
        {
            LeftCommands.Add(command);
            return this;
        }

        public BranchesManager AddRight(Command command)
        {
            RightCommands.Add(command);
            return this;
        }

        public CodeManager MergeBranches(UserDialog dialog)
        {
            var leftCommand = LeftCommands.Single();
            var rightCommand = RightCommands.Single();

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
        public Commands Commands => new Commands();
        public UserQuestion User => new UserQuestion(new List<KeyValuePair<string, string>>());
        public MessagesGenerator MessagesGenerator { get { return new MessagesGenerator(); } }
        public Utility Utility => new Utility();
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

        [TestMethod]
        public void MoveMethodUp()
        {
            var startCode = Library.GetCode("Ex_2");

            var methodUp = Commands.MoveMethodUp("GeneralCase", "MethodB");
            var rename = Commands.Rename("GeneralCase", "MethodB", "variable", "variable1");

            var resultCode =

            startCode
                .ForkBranches()
                .AddLeft(methodUp)
                .AddRight(rename)
                .MergeBranches(User.NotAsked)
                .Code;

            Assert.AreEqual(
                Utility.ApplyCommands(
                    startCode.Code,
                    methodUp,
                    rename),
                resultCode);
        }
    }
}
