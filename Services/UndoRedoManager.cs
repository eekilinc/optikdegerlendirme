using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Undo/Redo sistemi için command pattern implementasyonu
    /// </summary>
    public interface IUndoableCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
        void Redo();
    }

    public class UndoRedoManager
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();
        private readonly int _maxHistorySize;

        public event EventHandler? CanUndoChanged;
        public event EventHandler? CanRedoChanged;
        public event EventHandler<string>? CommandExecuted;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public UndoRedoManager(int maxHistorySize = 50)
        {
            _maxHistorySize = maxHistorySize;
        }

        public void ExecuteCommand(IUndoableCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // Yeni işlem yapılınca redo stack'i temizle
            
            // Max history limit
            if (_undoStack.Count > _maxHistorySize)
            {
                // Stack'i listeye çevirip baştan eleman çıkarıp tekrar stack yap
                var tempList = _undoStack.ToList();
                tempList.RemoveAt(tempList.Count - 1);
                _undoStack.Clear();
                foreach (var cmd in tempList.Reverse<IUndoableCommand>())
                    _undoStack.Push(cmd);
            }

            CommandExecuted?.Invoke(this, command.Description);
            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Redo();
            _undoStack.Push(command);

            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetUndoDescription() => CanUndo ? _undoStack.Peek().Description : "Geri alınacak işlem yok";
        public string GetRedoDescription() => CanRedo ? _redoStack.Peek().Description : "Yenilenecek işlem yok";
    }

    /// <summary>
    /// Öğrenci verisi değişikliği için Undo komutu
    /// </summary>
    public class StudentDataChangeCommand : IUndoableCommand
    {
        private readonly List<StudentResult> _target;
        private readonly List<StudentResult> _oldState;
        private List<StudentResult> _newState;
        public string Description { get; }

        public StudentDataChangeCommand(List<StudentResult> target, List<StudentResult> newState, string description)
        {
            _target = target;
            _newState = newState;
            Description = description;
            
            // Derin kopya - mevcut state'i kaydet
            _oldState = target.Select(s => new StudentResult
            {
                StudentId = s.StudentId,
                FullName = s.FullName,
                BookletType = s.BookletType,
                RawAnswers = s.RawAnswers,
                Answers = new List<string>(s.Answers),
                Score = s.Score,
                CorrectCount = s.CorrectCount,
                IncorrectCount = s.IncorrectCount,
                EmptyCount = s.EmptyCount,
                NetCount = s.NetCount,
                Rank = s.Rank,
                RowNumber = s.RowNumber,
                QuestionResults = new List<bool>(s.QuestionResults),
                ColoredAnswers = new ObservableCollection<AnswerItem>(s.ColoredAnswers.Select(a => new AnswerItem { Character = a.Character, State = a.State }))
            }).ToList();
        }

        public void Execute()
        {
            ApplyState(_newState);
        }

        public void Undo()
        {
            ApplyState(_oldState);
        }

        public void Redo()
        {
            ApplyState(_newState);
        }

        private void ApplyState(List<StudentResult> state)
        {
            _target.Clear();
            foreach (var student in state)
            {
                _target.Add(new StudentResult
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    BookletType = student.BookletType,
                    RawAnswers = student.RawAnswers,
                    Answers = new List<string>(student.Answers),
                    Score = student.Score,
                    CorrectCount = student.CorrectCount,
                    IncorrectCount = student.IncorrectCount,
                    EmptyCount = student.EmptyCount,
                    NetCount = student.NetCount,
                    Rank = student.Rank,
                    RowNumber = student.RowNumber,
                    QuestionResults = new List<bool>(student.QuestionResults),
                    ColoredAnswers = new ObservableCollection<AnswerItem>(student.ColoredAnswers.Select(a => new AnswerItem { Character = a.Character, State = a.State }))
                });
            }
        }
    }

    /// <summary>
    /// Cevap anahtarı değişikliği için Undo komutu
    /// </summary>
    public class AnswerKeyChangeCommand : IUndoableCommand
    {
        private readonly List<AnswerKeyModel> _target;
        private readonly List<AnswerKeyModel> _oldState;
        private readonly List<AnswerKeyModel> _newState;
        public string Description { get; }

        public AnswerKeyChangeCommand(List<AnswerKeyModel> target, List<AnswerKeyModel> newState, string description)
        {
            _target = target;
            _newState = newState;
            Description = description;
            
            _oldState = target.Select(k => new AnswerKeyModel
            {
                BookletName = k.BookletName,
                Answers = k.Answers
            }).ToList();
        }

        public void Execute() => ApplyState(_newState);
        public void Undo() => ApplyState(_oldState);
        public void Redo() => ApplyState(_newState);

        private void ApplyState(List<AnswerKeyModel> state)
        {
            _target.Clear();
            foreach (var key in state)
            {
                _target.Add(new AnswerKeyModel
                {
                    BookletName = key.BookletName,
                    Answers = key.Answers
                });
            }
        }
    }

    /// <summary>
    /// Öğrenim çıktısı (LearningOutcome) değişikliği için Undo komutu
    /// </summary>
    public class LearningOutcomeChangeCommand : IUndoableCommand
    {
        private readonly List<LearningOutcome> _target;
        private readonly string _oldState;
        private readonly string _newState;
        public string Description { get; }

        public LearningOutcomeChangeCommand(List<LearningOutcome> target, List<LearningOutcome> newState, string description)
        {
            _target = target;
            _newState = JsonSerializer.Serialize(newState);
            _oldState = JsonSerializer.Serialize(target);
            Description = description;
        }

        public void Execute() => ApplyState(_newState);
        public void Undo() => ApplyState(_oldState);
        public void Redo() => ApplyState(_newState);

        private void ApplyState(string jsonState)
        {
            var state = JsonSerializer.Deserialize<List<LearningOutcome>>(jsonState) ?? new List<LearningOutcome>();
            _target.Clear();
            foreach (var item in state)
                _target.Add(item);
        }
    }

    /// <summary>
    /// Soru ayarları (QuestionSetting) değişikliği için Undo komutu
    /// </summary>
    public class QuestionSettingChangeCommand : IUndoableCommand
    {
        private readonly List<QuestionSetting> _target;
        private readonly string _oldState;
        private readonly string _newState;
        public string Description { get; }

        public QuestionSettingChangeCommand(List<QuestionSetting> target, List<QuestionSetting> newState, string description)
        {
            _target = target;
            _newState = JsonSerializer.Serialize(newState);
            _oldState = JsonSerializer.Serialize(target);
            Description = description;
        }

        public void Execute() => ApplyState(_newState);
        public void Undo() => ApplyState(_oldState);
        public void Redo() => ApplyState(_newState);

        private void ApplyState(string jsonState)
        {
            var state = JsonSerializer.Deserialize<List<QuestionSetting>>(jsonState) ?? new List<QuestionSetting>();
            _target.Clear();
            foreach (var item in state)
                _target.Add(item);
        }
    }
}
