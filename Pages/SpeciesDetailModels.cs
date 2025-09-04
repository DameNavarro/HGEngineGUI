using CommunityToolkit.Mvvm.ComponentModel;

namespace HGEngineGUI.Pages
{
    public class LevelUpEntry : ObservableObject
    {
        private int _level;
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        private string _move = string.Empty;
        public string Move
        {
            get => _move;
            set => SetProperty(ref _move, value);
        }
    }

    public class EggMoveEntry : ObservableObject
    {
        private string _move = string.Empty;
        public string Move
        {
            get => _move;
            set => SetProperty(ref _move, value);
        }
    }

    public class EvolutionEntry : ObservableObject
    {
        private string _method = string.Empty;
        public string Method
        {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        private int _param;
        public int Param
        {
            get => _param;
            set => SetProperty(ref _param, value);
        }

        private string _target = string.Empty;
        public string Target
        {
            get => _target;
            set => SetProperty(ref _target, value);
        }

        private int _form;
        public int Form
        {
            get => _form;
            set => SetProperty(ref _form, value);
        }

        // Dynamic list of method conditions, each representing one EVO_* with its own param
        private System.Collections.ObjectModel.ObservableCollection<EvoCondition> _conditions = new();
        public System.Collections.ObjectModel.ObservableCollection<EvoCondition> Conditions => _conditions;
    }

    public class EvoCondition : ObservableObject
    {
        private string _method = string.Empty; // EVO_*
        public string Method
        {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        private int _param;
        public int Param
        {
            get => _param;
            set => SetProperty(ref _param, value);
        }
    }
}


