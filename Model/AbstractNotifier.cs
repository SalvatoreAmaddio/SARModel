using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SARModel
{
    public abstract class AbstractNotifier : IAbstractNotifier
    {
        public ValuesList Values { get; } = new(); 
        public event EventHandler<AbstractPropChangedEventArgs>? AfterUpdate;
        public event EventHandler<AbstractPropChangedEventArgs>? BeforeUpdate;
        public event PropertyChangedEventHandler? PropertyChanged;

        public static void Capitalise(ref AbstractPropChangedEventArgs e, ref string _backProp)
        {
            string? val = e.GetNewValue()?.ToString()?.Capitalise();
            if (val == null) return;
            _backProp = val;
        }

        public virtual void Set<T>(ref T value, ref T _backprop, [CallerMemberName] string propName = "")
        {
            PropChangedEventArgs<T> prop = new(ref value, ref _backprop, propName);
            Values.AddValue(prop);
            BeforeUpdate?.Invoke(this, prop);
            if (prop.Cancel) return;
            _backprop = value;
            AfterUpdate?.Invoke(this, prop);
            NotifyView(propName);
        }

        public void NotifyView(string propname) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));

        public void InvokeBeforeUpdate(AbstractPropChangedEventArgs e) =>
        BeforeUpdate?.Invoke(this, e);

        public void InvokeAfterUpdate(AbstractPropChangedEventArgs e) =>
        AfterUpdate?.Invoke(this, e);

        public virtual void Undo() {
           
            var props = GetType().GetProperties();
            foreach (var prop in props)
            {
                    object? old = Values?.FirstOrDefault(s => s.PropName.Equals(prop.Name))?.GetBackPropValue();
                    if (old != null)
                    prop.SetValue(this, old);
            }
        }
    }

    public class ValuesList : List<AbstractPropChangedEventArgs>
    {
        public void AddValue(AbstractPropChangedEventArgs value)
        {
            var exist = this.Any(s => s.Equals(value));
            if (exist) return;
            Add(value);
        }

        public AbstractPropChangedEventArgs Get(string propname) => this.First(s => s.PropIs(propname));
    }

}