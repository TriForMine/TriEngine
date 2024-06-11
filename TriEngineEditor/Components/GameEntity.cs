using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using TriEngineEditor.DllWrappers;
using TriEngineEditor.GameProject;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.Components
{
    [DataContract]
    [KnownType(typeof(Transform))]
    [KnownType(typeof(Script))]
    class GameEntity : ViewModelBase
    {
        private int _entityId = ID.INVALID_ID;

        public int EntityId
        {
            get => _entityId;
            set
            {
                if (value != _entityId)
                {
                    _entityId = value;
                    OnPropertyChanged(nameof(EntityId));
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value != _isActive)
                {
                    _isActive = value;

                    if (_isActive)
                    {
                        EntityId = EngineAPI.EntityAPI.CreateGameEntity(this);
                        Debug.Assert(ID.IsValid(EntityId));
                    }
                    else if(ID.IsValid(EntityId))
                    {
                        EngineAPI.EntityAPI.RemoveGameEntity(this);
                        EntityId = ID.INVALID_ID;
                    }

                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        private bool _isEnabled = true;
        [DataMember]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        private string _name;
        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [DataMember]
        public Scene ParentScene { get; private set; }

        [DataMember(Name = nameof(Components))]
        private readonly ObservableCollection<Component> _components = new ObservableCollection<Component>();
        public ReadOnlyObservableCollection<Component> Components { get; private set; }

        public Component GetComponent(Type type) => Components.FirstOrDefault(x => x.GetType() == type);
        public T GetComponent<T>() where T : Component => (T)GetComponent(typeof(T));

        public bool AddComponent(Component component)
        {
            Debug.Assert(component != null);

            if (!Components.Any(x => x.GetType() == component.GetType()))
            {
                IsActive = false;
                _components.Add(component);
                IsActive = true;
                return true;
            }

            Logger.Log(MessageType.Warning, $"Component of type {component.GetType()} already exists in entity {Name}");

            return false;
        }

        public void RemoveComponent(Component component)
        {
            Debug.Assert(component != null);
            if (component is Transform) return;

            if (_components.Contains(component))
            {
                IsActive = false;
                _components.Remove(component);
                IsActive = true;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_components != null)
            {
                Components = new ReadOnlyObservableCollection<Component>(_components);
                OnPropertyChanged(nameof(Components));
            }
        }

        public GameEntity(Scene scene)
        {
            Debug.Assert(scene != null);
            ParentScene = scene;
            _components.Add(new Transform(this));
            OnDeserialized(new StreamingContext());
        }
    }

    abstract class MSEntity : ViewModelBase
    {
        // Enables updates to selected entities
        private bool _enableUpdates = true;
        private bool? _isEnabled = true;
        [DataMember]
        public bool? IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        private string _name;
        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private readonly ObservableCollection<IMSComponent> _components = new ObservableCollection<IMSComponent>();
        public ReadOnlyObservableCollection<IMSComponent> Components { get; private set; }

        public T GetMSComponent<T>() where T : IMSComponent => (T)Components.FirstOrDefault(x => x is T);
        public List<GameEntity> SelectedEntities { get; }

        private void MakeComponentList()
        {
            _components.Clear();
            var firstEntity = SelectedEntities.FirstOrDefault();
            if (firstEntity == null)
            {
                return;
            }

            foreach (var component in firstEntity.Components)
            {
                var type = component.GetType();
                if (!SelectedEntities.Skip(1).Any(entity => entity.GetComponent(type) == null))
                {
                    Debug.Assert(Components.FirstOrDefault(x => x.GetType() == type) == null);
                    _components.Add(component.GetMultiselectionComponent(this));
                }
            }
        }

        public static float? GetMixedValue<T>(List<T> objects, Func<T, float> getProperty)
        {
            var value = getProperty(objects.First());
            return objects.Skip(1).Any(x => !value.IsTheSameAs(getProperty(x))) ? (float?)null : value;
        }

        public static bool? GetMixedValue<T>(List<T> objects, Func<T, bool> getProperty)
        {
            var value = getProperty(objects.First());
            return objects.Skip(1).Any(x => getProperty(x) != value) ? (bool?)null : value;
        }

        public static string? GetMixedValue<T>(List<T> objects, Func<T, string> getProperty)
        {
            var value = getProperty(objects.First());
            return objects.Skip(1).Any(x => getProperty(x) != value) ? null : value;
        }

        protected virtual bool UpdateGameEntities(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(IsEnabled): SelectedEntities.ForEach(x => x.IsEnabled = IsEnabled.Value); return true;
                case nameof(Name): SelectedEntities.ForEach(x => x.Name = Name); return true;
            }

            return false;
        }

        protected virtual bool UpdateMSGameEntities()
        {

            if (SelectedEntities.Count == 0)
            {
                return false;
            }

            IsEnabled = GetMixedValue(SelectedEntities, new Func<GameEntity, bool>(x => x.IsEnabled));
            Name = GetMixedValue(SelectedEntities, new Func<GameEntity, string>(x => x.Name));

            return true;
        }

        public void Refresh()
        {
            _enableUpdates = false;
            UpdateMSGameEntities();
            MakeComponentList();
            _enableUpdates = true;
        }

        public MSEntity(List<GameEntity> entities)
        {
            Debug.Assert(entities?.Any() == true);
            Components = new ReadOnlyObservableCollection<IMSComponent>(_components);
            SelectedEntities = entities;
            PropertyChanged += (sender, e) => { if (_enableUpdates) UpdateGameEntities(e.PropertyName); };
        }
    }

    class MSGameEntity : MSEntity
    {
        public MSGameEntity(List<GameEntity> entities) : base(entities)
        {
            Refresh();
        }
    }
}
