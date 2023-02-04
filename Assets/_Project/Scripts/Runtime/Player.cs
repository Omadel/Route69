using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Route69
{
    public class Player : Unit
    {
        public override string Name => playerName;

        [SerializeField] string playerName;
        [SerializeField] int startHealth = 30;
        [SerializeField] int attackDamage = 3;
        [SerializeField] Color hitColor = Color.black;

        Tween hitTween;
        private Vector3 _initPos;

        private void Start()
        {
            InvokeOnBossChanged(playerName);
            animator = GetComponentInChildren<Animator>();
            var animationListener = GetComponentInChildren<AnimationListener>();
            animationListener.OnAttack += Attack;
            SetHealth(startHealth);
            _initPos = transform.position;
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                PlayAttackAnimation();
            }
        }

        private void SetHealth(int health)
        {
            if (GameManagerUI.Instance.IsGameEnded) return;

            currentHealth = health;
            InvokeOnHealthChanged(health / (float)startHealth);

            if (currentHealth <= 0) LooseGame();
        }

        protected override void Die()
        {
            base.Die();
            GameManagerUI.Instance.Defeat();
            GameManager.Instance.CurrentBoss.SetState(Boss.State.Win);
        }

        private void LooseGame()
        {
            animator.Play("Knocked Out", 0, 0f);
            StartCoroutine(LooseRoutine());
            enabled = false;
        }

        private IEnumerator LooseRoutine()
        {
            yield return new WaitForSeconds(.5f);
            GameManagerUI.Instance.Defeat();
            yield return new WaitForSeconds(1.5f);
            Die();
        }

        public void PlayAttackAnimation()
        {
            animator.Play("Punch", 0, 0f);
        }

        private void Attack()
        {
            var pushDistance = GameManager.Instance.CurrentBoss.Hit(attackDamage);
            if (pushDistance <= -1)
            {
                StartCoroutine(Win());
                return;
            }

            transform.DOMoveZ(transform.position.z + pushDistance, .4f).SetDelay(.2f);
            ShakeObj.Instance.StartShakingCam(0.1f);
            ZoomCam.Instance.StartZoomingCam(0);
        }

        private IEnumerator Win()
        {
            yield return new WaitForSeconds(2f);
            animator.Play("Victory");
        }

        public void Hit(int damage, float push)
        {
            if (!enabled) return;
            animator.Play("Hit", 0, 0f);
            SetHealth(currentHealth - damage);
            var material = animator.GetComponentInChildren<Renderer>().material;
            const string colorName = "_FillColor";
            material.SetColor(colorName, hitColor);
            hitTween?.Complete();
            hitTween = DOTween.ToAlpha(() => material.GetColor(colorName), c => material.SetColor(colorName, c), 0f,
                .4f);
            transform.DOMoveZ(transform.position.z - push, .4f).SetEase(Ease.OutCirc);
            ShakeObj.Instance.StartShakingCam(0);
        }

        public void ResetPosPlayer()
        {
            gameObject.transform.position = _initPos;
        }
    }
}