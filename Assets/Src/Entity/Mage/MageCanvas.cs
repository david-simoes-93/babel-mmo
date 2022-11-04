using UnityEngine;
using UnityEngine.UI;

internal class MageCanvas : BaseCanvas
{
    private Image SpellFireflash_,
        SpellFrostflash_,
        SpellArcaneflash_,
        SpellPyroblast_,
        SpellRenew_;
    private Text HpText_,
        TargetText_;
    private Image HpBar_,
        TargetBar_,
        ChannelBar_;
    private MageCastValidator MageValidator_;
    private GameObject ChannelBarComponent_,
        TargetBarComponent_;
    private UnitEntity SelectedTarget_;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    internal override void Awake()
    {
        var spellTransform = transform.Find("SpellBar");
        SpellFireflash_ = spellTransform.Find("Spell 1").GetComponent<Image>();
        SpellFrostflash_ = spellTransform.Find("Spell 2").GetComponent<Image>();
        SpellArcaneflash_ = spellTransform.Find("Spell 3").GetComponent<Image>();
        SpellPyroblast_ = spellTransform.Find("Spell 4").GetComponent<Image>();
        SpellRenew_ = spellTransform.Find("Spell 5").GetComponent<Image>();

        var hpTransform = transform.Find("HpBar");
        HpText_ = hpTransform.Find("Text").GetComponent<Text>();
        HpBar_ = hpTransform.Find("Bar").GetComponent<Image>();

        var targetTransform = transform.Find("TargetBar");
        TargetBarComponent_ = targetTransform.gameObject;
        TargetBarComponent_.SetActive(false);
        TargetText_ = targetTransform.Find("Text").GetComponent<Text>();
        TargetBar_ = targetTransform.Find("Bar").GetComponent<Image>();

        var channelBarTransform = transform.Find("SpellChannelBar");
        ChannelBarComponent_ = channelBarTransform.gameObject;
        ChannelBarComponent_.SetActive(false);
        ChannelBar_ = channelBarTransform.Find("FrontBar").GetComponent<Image>();

        AlertAnchor_ = transform.Find("AlertAnchor").gameObject;

        SelectedTarget_ = null;
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    internal override void Update()
    {
        SetHP(parent_.Health);
        UpdateTarget();

        SpellFireflash_.fillAmount = 1 - MageValidator_.CooldownFireflash();
        SpellFrostflash_.fillAmount = 1 - MageValidator_.CooldownFrostflash();
        SpellArcaneflash_.fillAmount = 1 - MageValidator_.CooldownArcaneflash();
        SpellPyroblast_.fillAmount = 1 - MageValidator_.CooldownPyroblast();
        SpellRenew_.fillAmount = 1 - MageValidator_.CooldownRenew();

        if (MageValidator_.IsChannelingSpell())
        {
            ChannelBarComponent_.SetActive(true);
            ChannelBar_.transform.localScale = new Vector3(MageValidator_.ChannelingProgress(), 1, 1);
        }
        else
        {
            ChannelBarComponent_.SetActive(false);
        }
        // TODO change icon color if out of range or not target selected
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        MageValidator_ = parent.Validator as MageCastValidator;
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

            if (currentHp <= 0.2 * parent_.MaxHealth)
                HpBar_.color = Color.red;
            else if (currentHp <= 0.5 * parent_.MaxHealth)
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

    internal void SetTarget(UnitEntity target)
    {
        SelectedTarget_ = target;
        UpdateTarget();
        TargetBarComponent_.SetActive(true);
    }

    internal void ClearTarget()
    {
        SelectedTarget_ = null;
        TargetBarComponent_.SetActive(false);
    }

    internal UnitEntity GetTarget()
    {
        return SelectedTarget_;
    }

    internal void UpdateTarget()
    {
        if (SelectedTarget_ == null || !SelectedTarget_.IsTargetable)
        {
            ClearTarget();
            return;
        }
        TargetText_.text = SelectedTarget_.Name + " : " + SelectedTarget_.Health;
        if (SelectedTarget_.Health <= 0)
        {
            TargetBar_.color = Color.gray;
        }
        else
        {
            if (SelectedTarget_.Health <= 0.2 * SelectedTarget_.MaxHealth)
                TargetBar_.color = Color.red;
            else if (SelectedTarget_.Health <= 0.5 * SelectedTarget_.MaxHealth)
                TargetBar_.color = Color.yellow;
            else
                TargetBar_.color = Color.green;
        }
    }
}
