<svg class="sprite" aria-hidden="true" focusable="false">
	<defs>
		<linearGradient id="g-yk" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="100" y2="100">
			<stop offset="0" stop-color="#1b1640"/>
			<stop offset="1" stop-color="#07070f"/>
		</linearGradient>
		<linearGradient id="g-yk-c" gradientUnits="userSpaceOnUse" x1="0" y1="20" x2="0" y2="80">
			<stop offset="0" stop-color="#ffe14d"/>
			<stop offset="1" stop-color="#ff3d8b"/>
		</linearGradient>
		<filter id="f-yk-glow" x="-30%" y="-30%" width="160%" height="160%">
			<feGaussianBlur stdDeviation="2.6" result="blur"/>
			<feMerge>
				<feMergeNode in="blur"/>
				<feMergeNode in="SourceGraphic"/>
			</feMerge>
		</filter>
		<linearGradient id="g-sc" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="100" y2="100">
			<stop offset="0" stop-color="#ff3d8b"/>
			<stop offset="0.5" stop-color="#ff8a3d"/>
			<stop offset="1" stop-color="#7a5cff"/>
		</linearGradient>
		<radialGradient id="g-shine" cx="0.2" cy="0.1" r="0.9">
			<stop offset="0" stop-color="#fff" stop-opacity="0.35"/>
			<stop offset="0.6" stop-color="#fff" stop-opacity="0"/>
		</radialGradient>
	</defs>

	<!-- Знак автора: буква y из yakus и мигающий курсор, как на yakus.ru -->
	<symbol id="i-yk" viewBox="0 0 100 100">
		<rect width="100" height="100" rx="24" fill="url(#g-yk)"/>
		<path d="M26 30l14 24M54 30 34 76" fill="none" stroke="url(#g-yk-c)" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" filter="url(#f-yk-glow)"/>
		<rect x="62" y="62" width="14" height="16" rx="2" fill="url(#g-yk-c)" filter="url(#f-yk-glow)">
			<animate attributeName="opacity" values="1;1;0;0" keyTimes="0;0.5;0.5;1" dur="1.1s" repeatCount="indefinite"/>
		</rect>
	</symbol>

	<!-- ShotHost: рамка видоискателя и стрелка загрузки -->
	<symbol id="i-sc" viewBox="0 0 100 100">
		<rect width="100" height="100" rx="24" fill="url(#g-sc)"/>
		<rect width="100" height="100" rx="24" fill="url(#g-shine)"/>
		<path d="M24 40V30a6 6 0 0 1 6-6h10M60 24h10a6 6 0 0 1 6 6v10M76 60v10a6 6 0 0 1-6 6H60M40 76H30a6 6 0 0 1-6-6V60" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/>
		<path d="M50 66V36M38 47l12-12 12 12" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/>
	</symbol>

	<!-- Значки систем для ссылок скачивания: те же, что в /assets/os-icons.svg на yakus.ru, встроены, чтобы ShotHost работал и на других серверах -->
	<symbol id="i-apple" viewBox="0 0 24 24">
		<path fill="currentColor" d="M16.4 12.7c0-2.3 1.9-3.4 2-3.5-1.1-1.6-2.8-1.8-3.4-1.8-1.4-.1-2.8.9-3.5.9-.7 0-1.8-.8-3-.8-1.5 0-3 .9-3.8 2.3-1.6 2.8-.4 7 1.2 9.3.8 1.1 1.7 2.4 2.9 2.3 1.2 0 1.6-.7 3-.7s1.8.7 3 .7c1.2 0 2-1.1 2.8-2.3.9-1.3 1.2-2.5 1.3-2.6-.1 0-2.5-.9-2.5-3.8zM14.1 5.8c.6-.8 1.1-1.8 1-2.8-.9 0-2 .6-2.7 1.4-.6.7-1.1 1.7-1 2.7 1 .1 2-.5 2.7-1.3z"/>
	</symbol>
	<symbol id="i-windows" viewBox="0 0 24 24">
		<path fill="currentColor" d="M3 5.3 10.3 4.3v7.1H3zM11.2 4.2 21 2.8v8.6h-9.8zM3 12.4h7.3v7.2L3 18.6zM11.2 12.4H21v8.7l-9.8-1.4z"/>
	</symbol>
	<symbol id="i-github" viewBox="0 0 16 16">
		<path fill="currentColor" d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27s1.36.09 2 .27c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8z"/>
	</symbol>
</svg>

<header class="topbar">
	<div class="wrap topbar-inner">
		<a class="brand" href="<?= $site_url ?>">
			<svg class="brand-mark" aria-hidden="true"><use href="#i-yk"/></svg>
			<span><span class="brand-pre">igor</span>yakus</span>
		</a>
		<nav class="nav">
			<a href="<?= $home_url ?>#features"><?= t('nav_features') ?></a>
			<a href="<?= $home_url ?>#download"><?= t('nav_download') ?></a>
			<a href="<?= $home_url ?>#setup"><?= t('nav_setup') ?></a>
			<?php if (empty($is_home)): ?>
			<?php elseif ($lang === 'ru'): ?>
				<a class="lang" href="/shothost/" hreflang="en" lang="en" title="English version">EN / <b>RU</b></a>
			<?php else: ?>
				<a class="lang" href="/shothost/ru/" hreflang="ru" lang="ru" title="Русская версия"><b>EN</b> / RU</a>
			<?php endif; ?>
			<?php if (!empty($download_url)): ?>
				<a class="nav-cta" href="<?php echo htmlspecialchars($download_url); ?>" download><?= t('download_original') ?></a>
			<?php else: ?>
				<a class="nav-cta" href="https://github.com/igoryakus/shothost">GitHub</a>
			<?php endif; ?>
		</nav>
	</div>
</header>
