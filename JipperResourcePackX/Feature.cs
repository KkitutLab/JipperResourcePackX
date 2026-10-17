using System;

namespace JipperResourcePackX;

public abstract class Feature {
    public string Name { get; protected set; }
    public bool Enabled { get; set; } = true;

    protected Feature(string name, bool enabled = true) {
        Name = name;
        Enabled = enabled;
    }

    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnGUI() { }
    public virtual void OnHideGUI() { }
    public virtual void OnUpdate(float deltaTime) { }
}
