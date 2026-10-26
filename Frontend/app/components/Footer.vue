<template>
  <footer>
    <div class="first-circle">
      <div class="second-circle">
        <div class="content-wrapper">
          <div class="social">
            <h5 class="inverse">Наши соцсети:</h5>
            <div class="social-links inverse">
              <a href="https://www.youtube.com/@multi-punk">
                <Youtube />
              </a>
              <a href="https://discord.gg/BnjZNRybqM">
                <Discord />
              </a>
              <a href="https://t.me/multipunk">
                <Telegram />
              </a>
            </div>
          </div>
          <div class="grid">
            <div class="support-links">
              <h5 class="inverse">Поддержка:</h5>
              <div>
                <a href="https://t.me/MultiPunkManager">
                  <h6 class="bluish">Telegram - менеджер</h6>
                </a>
                <a href="mailto:core@multi-punk.net">
                  <h6 class="bluish">Email - core@multi-punk.net</h6>
                </a>
                <a href="https://discord.com/channels/1141466506786517043/1274408858319851632">
                  <h6 class="bluish">Discord - тикеты</h6>
                </a>
              </div>
            </div>
            <div class="documents-links">
              <h5 class="inverse">Документы:</h5>
              <div>
                <a href="https://multi-punk.net/documents/licenseAgreement">
                  <h6 class="bluish">Пользовательское соглашение</h6>
                </a>
                <a href="https://multi-punk.net/documents/privacyPolicy">
                  <h6 class="bluish">Политика конфиденциальности</h6>
                </a>
                <a href="https://multi-punk.net/documents/termsOfService">
                  <h6 class="bluish">Публичная оферта</h6>
                </a>
              </div>
            </div>
            <div class="legal-info">
              <div class="inverse logo">
                <MultiPunk />
              </div>
              <div>
                <h6 class="bluish">© 2026 MultiPunk</h6>
                <h6 class="bluish">Все права защищены</h6>
                <h6 class="bluish">Портал MultiPunk не имеет отношения к Mojang AB.</h6>
                <h6><br /></h6>
                <h6 class="bluish">Самозанятый: Едемский Артём Антонович</h6>
                <h6 class="bluish">ИНН: 780264719278</h6>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </footer>
</template>

<script setup lang="ts">
import Youtube from "~/assets/images/svg/youtube.svg";
import Discord from "~/assets/images/svg/discord.svg";
import Telegram from "~/assets/images/svg/telegram-flat.svg";
import MultiPunk from "~/assets/images/svg/logo.svg";
</script>

<style scoped lang="less">
footer {
  height: 500px;
  overflow: hidden;
  margin: auto calc(var(--page-padding) * -1) 0 calc(var(--page-padding) * -1);

  a {
    cursor: pointer;
  }

  .first-circle {
    top: 50px;
    left: 50%;
    transform: translate(-50%, 0);

    position: relative;
    width: 500vw;
    height: 500vw;

    display: flex;

    border-radius: 50%;
    background: linear-gradient(
      105.43deg,
      var(--scolor-accent) 25.53%,
      var(--scolor-cyan) 42.22%,
      var(--scolor-footer-gradient-end) 55.2%
    );

    padding: 40px 0 0 0;
  }

  .second-circle {
    display: flex;
    justify-content: center;

    width: 500vw;
    height: 500vw;

    margin: 0 auto;
    border-radius: 100%;

    background: var(--scolor-footer-bg);
  }

  .content-wrapper {
    gap: 20px;
    display: flex;
    padding: 40px 20px;
    flex-direction: column;

    width: 100vw;
  }

  .social {
    align-items: center;
    justify-content: center;
  }

  .social,
  .legal-info,
  .support-links,
  .documents-links {
    gap: 10px;
    display: flex;
    flex-direction: column;
  }

  .social-links {
    gap: 15px;
    display: flex;
  }

  .legal-info {
    grid-area: legal;
  }

  .support-links {
    grid-area: support;
  }

  .documents-links {
    grid-area: documents;
  }

  .grid {
    margin: 0 auto;
    display: grid;
    grid-template-columns: 1fr 1fr;
    grid-template-areas:
      "support documents"
      "legal legal";

    justify-content: center;

    gap: 40px;
  }

  .logo {
    width: 102px;
    height: 17px;

    svg {
      width: 100%;
      height: 100%;
      margin-right: auto;
    }
  }
}
</style>
