using UnityEngine;
using UnityEngine.UI;

internal class FighterCanvas : BaseCanvas
{
    private Image[] Combo_;
    private Image SpellAttackLeft_,
        SpellAttackRight_,
        SpellCharge_,
        SpellDodge_;
    private Sprite ComboSpin_,
        ComboEmpty_;
    private Text HpText_;
    private Image HpBar_;
    private FighterCastValidator FighterValidator_;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    internal override void Awake()
    {
        Combo_ = new Image[5];
        var comboTransform = transform.Find("ComboBar");
        Combo_[0] = comboTransform.Find("Combo 1").GetComponent<Image>();
        Combo_[1] = comboTransform.Find("Combo 2").GetComponent<Image>();
        Combo_[2] = comboTransform.Find("Combo 3").GetComponent<Image>();
        Combo_[3] = comboTransform.Find("Combo 4").GetComponent<Image>();
        Combo_[4] = comboTransform.Find("Combo 5").GetComponent<Image>();

        var spellTransform = transform.Find("SpellBar");
        SpellAttackLeft_ = spellTransform.Find("Spell 1").GetComponent<Image>();
        SpellAttackRight_ = spellTransform.Find("Spell 2").GetComponent<Image>();
        SpellCharge_ = spellTransform.Find("Spell 3").GetComponent<Image>();
        SpellDodge_ = spellTransform.Find("Spell 4").GetComponent<Image>();

        var hpTransform = transform.Find("HpBar");
        HpText_ = hpTransform.Find("Text").GetComponent<Text>();
        HpBar_ = hpTransform.Find("Bar").GetComponent<Image>();

        AlertAnchor_ = transform.Find("AlertAnchor").gameObject;

        var comboSpritesTransform = transform.Find("ComboSprites");
        ComboSpin_ = comboSpritesTransform.Find("Spin").GetComponent<Image>().sprite;
        ComboEmpty_ = comboSpritesTransform.Find("Empty").GetComponent<Image>().sprite;
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    internal override void Update()
    {
        SetHP(parent_.Health);

        SpellAttackLeft_.fillAmount = 1 - FighterValidator_.CooldownAttackLeft();
        SpellAttackRight_.fillAmount = 1 - FighterValidator_.CooldownAttackRight();
        SpellCharge_.fillAmount = 1 - FighterValidator_.CooldownCharge();
        SpellDodge_.fillAmount = 1 - FighterValidator_.CooldownDodge();
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        FighterValidator_ = parent.Validator as FighterCastValidator;
    }

    /// <summary>
    /// updates player HP on GUI
    /// </summary>
    /// <param name="hp">target HP</param>
    internal override void SetHP(int currentHp)
    {
        currentHp_ = currentHp;

        HpText_.text = currentHp.ToString();
        if (currentHp <= 0)
        {
            SetDead();
            HpBar_.color = Color.gray;
        }
        else
        {
            SetAlive();

            if (currentHp <= 20)
                HpBar_.color = Color.red;
            else if (currentHp <= 50)
                HpBar_.color = Color.yellow;
            else
                HpBar_.color = Color.green;
        }
    }

    /// <summary>
    /// Sets unit alive
    /// </summary>
    internal override void SetAlive()
    {
        if (deathPanel_ != null)
        {
            Destroy(deathPanel_);
            deathPanel_ = null;
        }
    }

    /// <summary>
    /// Sets unit dead
    /// </summary>
    internal override void SetDead()
    {
        if (deathPanel_ == null)
        {
            deathPanel_ = Instantiate(Globals.kDeathPanelPrefab);
            deathPanel_.transform.SetParent(AlertAnchor_.transform);
            deathPanel_.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Updates the Fighter's combo status, setting a given attack at an index in the combo
    /// </summary>
    /// <param name="index">the index of target combo box</param>
    /// <param name="cc">the fighter's attack</param>
    internal void SetAttackCombo(int index, Globals.CastCode cc)
    {
        switch (cc)
        {
            case Globals.CastCode.FighterAttackLeft:
                Combo_[index].sprite = SpellAttackLeft_.sprite;
                break;
            case Globals.CastCode.FighterAttackRight:
                Combo_[index].sprite = SpellAttackRight_.sprite;
                break;
            case Globals.CastCode.Spin:
                Combo_[index].sprite = ComboSpin_;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Reset all combo attacks
    /// </summary>
    internal void ResetCombo()
    {
        for (int i = 0; i < 5; i++)
        {
            Combo_[i].sprite = ComboEmpty_;
        }
    }

    /// <summary>
    /// adds local graphical fx (like combat text or explosions)
    /// </summary>
    /// <param name="rd">the cause of the FX</param>
    /// <param name="target">the target's UnitEntity</param>
    internal override void AddCombatEffect(CombatEffectRD rd, UnitEntity target)
    {
        FloatingText3D cmbtText;
        if (rd.target_uid == parent_.Uid)
        {
            if (Globals.IsHealingSpell(rd.effect_source_type))
            {
                cmbtText = new FloatingText3D(target.TargetingTransform.position, rd.value.ToString(), 1000, Color.green);
            }
            else
            {
                cmbtText = new FloatingText3D(target.TargetingTransform.position, rd.value.ToString(), 1000, Color.red);
            }
        }
        else
        {
            cmbtText = new FloatingText3D(target.TargetingTransform.position, rd.value.ToString(), 1000, Color.yellow);
        }

        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(cmbtText);
    }
}
