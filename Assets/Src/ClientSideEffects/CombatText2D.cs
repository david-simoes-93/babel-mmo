using UnityEngine;
using UnityEditor;

/// <summary>
/// A 2d text with an entity name and associated damage
/// </summary>
internal class CombatText2D : ILocalEffect
{
    internal Entity remote_entity_;
    internal RectTransform myRect;

    private GameObject myGameObject_;
    private UnityEngine.UI.Text myText_;
    internal Globals.CastCode cast_;
    private int currDamage_;

    /// <summary>
    /// Constructs text
    /// </summary>
    /// <param name="remote">The remote unit entity</param>
    /// <param name="dmg">Corresponding damage taken/dealt</param>
    /// <param name="color">Text color</param>
    /// <param name="parent">The panel containing this text</param>
    /// <param name="cast">The cast that caused this</param>
    internal CombatText2D(Entity remote, int dmg, Color color, Transform parent, Globals.CastCode cast)
    {
        remote_entity_ = remote;
        currDamage_ = dmg;
        cast_ = cast;
        myGameObject_ = new GameObject(remote.Uid + "-" + remote.Name, typeof(RectTransform));
        myRect = myGameObject_.transform.GetComponent<RectTransform>();

        myText_ = myGameObject_.AddComponent<UnityEngine.UI.Text>();

        myText_.text = remote.Name + " " + dmg;
        myText_.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        myText_.color = color;
        myText_.alignment = TextAnchor.MiddleCenter;
        myText_.horizontalOverflow = HorizontalWrapMode.Overflow;

        myGameObject_.transform.SetParent(parent);
        myRect.localPosition = Vector3.zero;
        myRect.sizeDelta = new Vector2(300, 20);
    }

    /// <summary>
    /// Fades text out and destroys it when transparent
    /// </summary>
    /// <returns></returns>
    public bool Update()
    {
        myText_.color = new Color(myText_.color.r, myText_.color.g, myText_.color.b, myText_.color.a - 0.01f);

        if (myText_.color.a <= 0)
        {
            Object.Destroy(myGameObject_);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Increases the damage associated with source/target
    /// </summary>
    /// <param name="dmg">Damage to add to previous value</param>
    /// <param name="color">Text color</param>
    internal void UpdateDamage(int dmg, Color color)
    {
        currDamage_ += dmg;
        myText_.text = remote_entity_.Name + " " + currDamage_;
        myText_.color = color;
    }
}
