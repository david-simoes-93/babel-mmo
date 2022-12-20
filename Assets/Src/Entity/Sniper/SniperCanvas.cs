using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Globals;

internal class SniperCanvas : BaseCanvas
{
    private Text hpText_,
        weaponRifleAmmo_,
        weaponShotgunAmmo_,
        weaponMedigunAmmo_;
    private Image hpBar_,
        weaponRifle_,
        weaponShotgun_,
        weaponMedigun_;
    private GameObject DamageIndicator_,
        CrossHair_;
    private GameObject CombatDamageLeft_,
        CombatDamageRight_;
    private int damageIndicatorFrameCounter_ = 0;
    private List<CombatText2D> currDmgMessagesLeft_ = new List<CombatText2D>();
    private List<CombatText2D> currDmgMessagesRight_ = new List<CombatText2D>();
    private SniperCastValidator validator_;

    private List<Image> poisonFx_ = new List<Image>();
    private float currentPoisonAlpha = 0;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    internal override void Awake()
    {
        var hpTransform = transform.Find("HpBar");
        hpText_ = hpTransform.Find("Text").GetComponent<Text>();
        hpBar_ = hpTransform.Find("Bar").GetComponent<Image>();

        var weaponsTransform = transform.Find("Weapons");
        var weaponRifleTransform = weaponsTransform.Find("WeaponOne");
        weaponRifle_ = weaponRifleTransform.GetComponent<Image>();
        weaponRifleAmmo_ = weaponRifleTransform.Find("Ammo").GetComponent<Text>();
        var weaponShotgunTransform = weaponsTransform.Find("WeaponTwo");
        weaponShotgun_ = weaponShotgunTransform.GetComponent<Image>();
        weaponShotgunAmmo_ = weaponShotgunTransform.Find("Ammo").GetComponent<Text>();
        var weaponMedigunTransform = weaponsTransform.Find("WeaponThree");
        weaponMedigun_ = weaponMedigunTransform.GetComponent<Image>();
        weaponMedigunAmmo_ = weaponMedigunTransform.Find("Ammo").GetComponent<Text>();

        DamageIndicator_ = transform.Find("DamageIndicator").gameObject;
        CrossHair_ = transform.Find("CrossHair").gameObject;
        AlertAnchor_ = transform.Find("AlertAnchor").gameObject;
        CombatDamageLeft_ = transform.Find("CombatDamageLeft").gameObject;
        CombatDamageRight_ = transform.Find("CombatDamageRight").gameObject;

        var PoisonZoneUiFxTransform = transform.Find("PoisonZoneUiFx");
        string[] poisonFxNames = { "Down", "Up", "Left", "Right" };
        foreach (string name in poisonFxNames)
        {
            var ImageComponent = PoisonZoneUiFxTransform.Find(name);
            poisonFx_.Add(ImageComponent.GetComponent<Image>());
        }
        UpdatePoisonZoneFxAlpha();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    internal override void Update()
    {
        SetHP(parent_.Health);

        damageIndicatorFrameCounter_++;
        if (damageIndicatorFrameCounter_ > 3)
            DamageIndicator_.SetActive(false);

        UpdateCombatText(currDmgMessagesLeft_);
        UpdateCombatText(currDmgMessagesRight_);

        if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponShotgun)
        {
            weaponRifle_.color = Color.gray;
            weaponShotgun_.color = Color.white;
            weaponMedigun_.color = Color.gray;
        }
        else if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponMedigun)
        {
            weaponRifle_.color = Color.gray;
            weaponShotgun_.color = Color.gray;
            weaponMedigun_.color = Color.white;
        }
        else
        {
            weaponRifle_.color = Color.white;
            weaponShotgun_.color = Color.gray;
            weaponMedigun_.color = Color.gray;
        }
        weaponRifle_.fillAmount = 1 - validator_.CooldownWeaponRifle();
        weaponRifleAmmo_.text = validator_.currAmmoRifle_.ToString() + "/" + SniperCastValidator.weapon_configs[CastCode.SniperChooseWeaponRifle].kMaxAmmo.ToString();
        weaponShotgun_.fillAmount = 1 - validator_.CooldownWeaponShotgun();
        weaponShotgunAmmo_.text =
            validator_.currAmmoShotgun_.ToString() + "/" + SniperCastValidator.weapon_configs[CastCode.SniperChooseWeaponShotgun].kMaxAmmo.ToString();
        weaponMedigun_.fillAmount = 1 - validator_.CooldownWeaponMedigun();
        weaponMedigunAmmo_.text =
            validator_.currAmmoMedigun_.ToString() + "/" + SniperCastValidator.weapon_configs[CastCode.SniperChooseWeaponMedigun].kMaxAmmo.ToString();

        UpdatePoisonZoneFxAlpha();
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        validator_ = parent.Validator as SniperCastValidator;
    }

    /// <summary>
    /// Updates the current combat text box
    /// </summary>
    /// <param name="currDmgMessages"></param>
    internal void UpdateCombatText(List<CombatText2D> currDmgMessages)
    {
        for (int i = 0; i < currDmgMessages.Count; i++)
        {
            if (currDmgMessages[i].Update())
            {
                currDmgMessages.Remove(currDmgMessages[i]);
                i--;
            }

            if (i > 0)
            {
                Vector3 currPos = currDmgMessages[i].myRect.localPosition;
                float prevTextY = currDmgMessages[i - 1].myRect.localPosition.y;

                if (Math.Abs(currPos.y - prevTextY) < 22)
                {
                    currDmgMessages[i].myRect.localPosition = new Vector3(currPos.x, currPos.y - 3, currPos.z);
                }
            }
        }
    }

    /// <summary>
    /// updates player HP on GUI
    /// </summary>
    /// <param name="hp">target HP</param>
    internal override void SetHP(int currentHp)
    {
        currentHp_ = currentHp;

        hpText_.text = currentHp.ToString();
        if (currentHp <= 0)
        {
            hpBar_.color = Color.gray;
            SetDead();
        }
        else
        {
            SetAlive();
            if (currentHp <= 20)
                hpBar_.color = Color.red;
            else if (currentHp <= 50)
                hpBar_.color = Color.yellow;
            else
                hpBar_.color = Color.green;
        }
    }

    /// <summary>
    /// Sets unit alive
    /// </summary>
    internal override void SetAlive()
    {
        CrossHair_.SetActive(true);
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
        CrossHair_.SetActive(false);
        if (deathPanel_ == null)
        {
            deathPanel_ = Instantiate(kDeathPanelPrefab);
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
        if (rd.target_uid == parent_.Uid)
        {
            AddSelfCombatEffect(rd);
        }
        else
        {
            AddExternalCombatEffect(rd, target);
        }
    }

    /// <summary>
    /// adds local graphical fx for stuff happening to the sniper
    /// </summary>
    /// <param name="rd">the cause of the FX</param>
    private void AddSelfCombatEffect(CombatEffectRD rd)
    {
        Entity source;
        if (rd.effect_source_uid == parent_.Uid)
            source = parent_;
        else
            source = ClientGameLoop.CGL.GameWorld.EntityManager.FindEntityByUid(rd.effect_source_uid);

        if (currDmgMessagesRight_.Count > 0 && currDmgMessagesRight_[0].remote_entity_.Uid == source.Uid && currDmgMessagesRight_[0].cast_ == rd.effect_source_type)
        {
            currDmgMessagesRight_[0].UpdateDamage(rd.value, IsHealingSpell(rd.effect_source_type) ? Color.green : Color.yellow);
        }
        else
        {
            CombatText2D cmbtText = new CombatText2D(
                source,
                rd.value,
                IsHealingSpell(rd.effect_source_type) ? Color.green : Color.red,
                CombatDamageRight_.transform,
                rd.effect_source_type
            );
            currDmgMessagesRight_.Insert(0, cmbtText);
        }

        if (rd.effect_source_type == CastCode.FreyjaPoisonZoneTick)
        {
            currentPoisonAlpha += 0.3f;
            if (currentPoisonAlpha > 1)
            {
                currentPoisonAlpha = 1;
            }
        }
    }

    /// <summary>
    /// adds local graphical fx for stuff happening other entities, not the sniper
    /// </summary>
    /// <param name="rd">the cause of the FX</param>
    /// <param name="target">the target's UnitEntity</param>
    private void AddExternalCombatEffect(CombatEffectRD rd, UnitEntity target)
    {
        DamageIndicator_.SetActive(true);
        damageIndicatorFrameCounter_ = 0;

        // If WeaponRifleAlternate's explosion
        if (rd.effect_source_type == CastCode.SniperWeaponRifleAlternate)
        {
            FloatingText3D cmbtText = new FloatingText3D(target.TargetingTransform.position, rd.value.ToString(), 1000, Color.yellow);
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(cmbtText);
        }

        if (currDmgMessagesLeft_.Count > 0 && currDmgMessagesLeft_[0].remote_entity_.Uid == target.Uid && currDmgMessagesLeft_[0].cast_ == rd.effect_source_type)
        {
            currDmgMessagesLeft_[0].UpdateDamage(rd.value, IsHealingSpell(rd.effect_source_type) ? Color.green : Color.yellow);
        }
        else
        {
            CombatText2D cmbtText = new CombatText2D(
                target,
                rd.value,
                IsHealingSpell(rd.effect_source_type) ? Color.green : Color.yellow,
                CombatDamageLeft_.transform,
                rd.effect_source_type
            );
            currDmgMessagesLeft_.Insert(0, cmbtText);
        }
    }

    private void UpdatePoisonZoneFxAlpha()
    {
        currentPoisonAlpha -= 0.01f;
        if (currentPoisonAlpha < 0)
        {
            currentPoisonAlpha = 0;
        }

        foreach (var poisonImage in poisonFx_)
        {
            poisonImage.color = new Color(1f, 1f, 1f, currentPoisonAlpha);
        }
    }
}
