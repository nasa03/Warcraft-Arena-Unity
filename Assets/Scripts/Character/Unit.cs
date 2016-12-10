﻿using System;
using UnityEngine;
using System.Collections.Generic;

public enum Team { Red, Blue }

public class Unit : MonoBehaviour
{
    public int id;
    public string unitName;
    public bool isHumanPlayer;
    public bool isDead;
    public bool isGrounded;
    public Character character;
    public Transform castPoint;
    public Transform centerPoint;
    public Team team;

    public float moveTimer = 0;
    public float castTimer = 0;

    public int Id 
    {
        get { return id; }
        set { id = value; }
    }
    public Vector3 Position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    private CapsuleCollider unitCollider;
    private Animator animator;

    public bool IsHumanPlayer
    { 
        get { return isHumanPlayer; }
        set { isHumanPlayer = value; }
    }
    public bool IsGrounded 
    {
        get { return isGrounded; }
        set
        {
            isGrounded = value;
        }
    }
    public bool IsMovementBlocked
    {
        get
        {
            return Character.states[EntityStateType.Root].InEffect || Character.states[EntityStateType.Stun].InEffect;
        }
    }

    public CapsuleCollider UnitCollider { get { return unitCollider; } }
    public Character Character { get { return character; } }
    public Animator Animator { get { return animator; } }

    void Awake()
    {
        animator = GetComponent<Animator>();
        unitCollider = GetComponent<CapsuleCollider>();

        Character.Initialize(this);
    }

    void Update()
    {
        UpdateUnit(ArenaManager.Instanse);
    }

    public void UpdateUnit(ArenaManager world)
    {
        if (moveTimer > 0)
            moveTimer -= Time.deltaTime;

        Character.Update(this, world);
    }

    public void TriggerInstantCast()
    {
        // Switch leg animation for casting
        if (animator.GetBool("Grounded"))
        {
            if (animator.GetFloat("Speed") > 0.1f)
                animator.Play("Run", 1);
            else
                animator.Play("Cast", 1);
        }
        else
            animator.Play("Air", 1);
    }

    #region Reputation

    public bool IsHostileTo(Unit unit)
    {
        return team != unit.team;
    }

    #endregion

    #region State Info

    public bool IsAlive() { return (Character.DeathState == DeathState.Alive); }

    public bool IsDying() { return (Character.DeathState == DeathState.JustDied); }

    public bool IsDead() { return (Character.DeathState == DeathState.Dead || Character.DeathState == DeathState.Corpse); }

    #endregion

    #region Spells

    public SpellMissInfo SpellHitResult(Unit victim, TrinitySpellInfo spellInfo, bool canReflect)
    {
        // Check for immune
        /*if (victim->IsImmunedToSpell(spellInfo))
            return SPELL_MISS_IMMUNE;*/

        // All positive spells can`t miss
        /// @todo client not show miss log for this spells - so need find info for this in dbc and use it!
        if (spellInfo.IsPositive() && !IsHostileTo(victim)) // prevent from affecting enemy by "positive" spell
            return SpellMissInfo.NONE;

        // Check for immune
        /*if (victim->IsImmunedToDamage(spellInfo))
            return SPELL_MISS_IMMUNE;*/

        if (this == victim)
            return SpellMissInfo.NONE;

        // Try victim reflect spell
        /*if (CanReflect)
        {
            int32 reflectchance = victim->GetTotalAuraModifier(SPELL_AURA_REFLECT_SPELLS);
                Unit::AuraEffectList const& mReflectSpellsSchool = victim->GetAuraEffectsByType(SPELL_AURA_REFLECT_SPELLS_SCHOOL);
            for (Unit::AuraEffectList::const_iterator i = mReflectSpellsSchool.begin(); i != mReflectSpellsSchool.end(); ++i)
                if ((*i)->GetMiscValue() & spellInfo->GetSchoolMask())
                    reflectchance += (*i)->GetAmount();
            if (reflectchance > 0 && roll_chance_i(reflectchance))
            {
                // Start triggers for remove charges if need (trigger only for victim, and mark as active spell)
                ProcDamageAndSpell(victim, PROC_FLAG_NONE, PROC_FLAG_TAKEN_SPELL_MAGIC_DMG_CLASS_NEG, PROC_EX_REFLECT, 1, BASE_ATTACK, spellInfo);
                return SPELL_MISS_REFLECT;
            }
        }*/

        /*switch (spellInfo->DmgClass)
        {
            case SPELL_DAMAGE_CLASS_RANGED:
            case SPELL_DAMAGE_CLASS_MELEE:
                return MeleeSpellHitResult(victim, spellInfo);
            case SPELL_DAMAGE_CLASS_NONE:
                return SPELL_MISS_NONE;
            case SPELL_DAMAGE_CLASS_MAGIC:
                return MagicSpellHitResult(victim, spellInfo);
        }*/
        return SpellMissInfo.NONE;
    }


    public float GetSpellMinRangeForTarget(Unit target, TrinitySpellInfo spellInfo)
    {
        if (spellInfo.Range == null)
            return 0;
        if (spellInfo.Range.MinRangeFriend == spellInfo.Range.MinRangeHostile)
            return spellInfo.GetMinRange(false);
        if (target != null)
            return spellInfo.GetMinRange(true);
        return spellInfo.GetMinRange(!IsHostileTo(target));
    }

    public float GetSpellMaxRangeForTarget(Unit target, TrinitySpellInfo spellInfo)
    {
        if (spellInfo.Range == null)
            return 0;
        if (spellInfo.Range.MaxRangeFriend == spellInfo.Range.MaxRangeHostile)
            return spellInfo.GetMaxRange(false);
        if (!target)
            return spellInfo.GetMaxRange(true);
        return spellInfo.GetMaxRange(!IsHostileTo(target));
    }


    public void CastSpell(SpellCastTargets targets, TrinitySpellInfo spellInfo, TriggerCastFlags triggerFlags, AuraEffect triggeredByAura, Guid originalCaster)
    {
        if (spellInfo == null)
        {
            Debug.LogError("Unknown spell for unit: " + gameObject.name);
            return;
        }

        TrinitySpell spell = new TrinitySpell(this, spellInfo, triggerFlags, originalCaster);
        spell.Prepare(targets, triggeredByAura);
    }

    public void CastSpell(Unit target, int spellId, bool triggered, AuraEffect triggeredByAura, Guid originalCaster)
    {
        CastSpell(target, spellId, triggered ? TriggerCastFlags.FULL_MASK : TriggerCastFlags.NONE, triggeredByAura, originalCaster);
    }

    public void CastSpell(Unit target, int spellId, TriggerCastFlags triggerFlags, AuraEffect triggeredByAura, Guid originalCaster)
    {
        TrinitySpellInfo spellInfo = WarcraftDatabase.SpellInfos.ContainsKey(spellId) ? WarcraftDatabase.SpellInfos[spellId] : null;
        if (spellInfo == null)
        {
            Debug.LogError("Unknown spell for unit: " + gameObject.name);
            return;
        }

        CastSpell(target, spellInfo, triggerFlags, triggeredByAura, originalCaster);
    }

    public void CastSpell(Unit target, TrinitySpellInfo spellInfo, bool triggered, AuraEffect triggeredByAura, Guid originalCaster)
    {
        CastSpell(target, spellInfo, triggered ? TriggerCastFlags.FULL_MASK : TriggerCastFlags.NONE, triggeredByAura, originalCaster);
    }

    public void CastSpell(Unit target, TrinitySpellInfo spellInfo, TriggerCastFlags triggerFlags, AuraEffect triggeredByAura, Guid originalCaster)
    {
        SpellCastTargets targets = new SpellCastTargets();
        targets.UnitTarget = target;
        CastSpell(targets, spellInfo, triggerFlags, triggeredByAura, originalCaster);
    }

    #endregion
}