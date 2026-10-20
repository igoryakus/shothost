<?php
// Получаем текущий URL и вытаскиваем из него имя запрашиваемого файла
$request_uri = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
// Отрезаем базовый путь /shothost/ если он есть
$base_path = '/shothost/';
$relative_path = $request_uri;

if (strpos($request_uri, $base_path) === 0) {
    $relative_path = substr($request_uri, strlen($base_path));
}

// Язык: /shothost/ru/ это русская версия, всё остальное английская (см. lang.php)
require_once __DIR__ . '/lang.php';
$lang = shothost_lang($relative_path);
shothost_load_texts($lang);
$home_url = $lang === 'ru' ? '/shothost/ru/' : '/shothost/';
$site_url = $lang === 'ru' ? 'https://yakus.ru/ru/' : 'https://yakus.ru/';

$clean_name = trim($relative_path, '/');
// Если после очистки осталось index.php, то это тоже главная
if ($clean_name === 'index.php') {
    $clean_name = '';
}

// Ищем файл с расширением .png
$image_file = $clean_name . '.png';
$image_path = __DIR__ . '/files/' . $image_file;

// Определяем, показываем мы картинку или главную страницу
$is_home = empty($clean_name);

// Проверяем наличие файла
$file_exists = !empty($clean_name) && file_exists($image_path);

// URL самой картинки для отображения (если это не главная)
$image_url = $is_home ? "" : "/shothost/files/" . rawurlencode($image_file);

// Параметры для общих head.php, header.php и footer.php
$page_title = $is_home ? t('title') : t('title_screenshot') . $clean_name;
$page_description = $is_home ? t('description') : "";
$body_class = $is_home ? "home-page" : "viewer-page";
$use_viewer = !$is_home;
$download_url = $file_exists ? $image_url : "";

include __DIR__ . '/head.php';
include __DIR__ . '/header.php';
?>

<?php if ($is_home): ?>
<main>
	<section class="product hero" id="about">
		<div class="wrap product-grid">
			<div>
				<svg class="tile tile-lg" aria-hidden="true"><use href="#i-sc"/></svg>
				<h1 class="product-title">ShotHost</h1>
				<p class="product-tagline"><?= t('tagline') ?></p>
				<p class="product-text"><?= t('intro') ?></p>
				<p class="product-text"><?= t('intro_app') ?></p>
				<div class="chips">
					<span class="chip"><?= t('chip_app') ?></span>
					<span class="chip">Windows</span>
					<span class="chip"><?= t('chip_vps') ?></span>
					<span class="chip"><?= t('chip_open_source') ?></span>
				</div>
				<div class="hero-actions">
					<a class="btn btn-primary" href="#download"><?= t('download') ?></a>
					<a class="btn btn-ghost" href="#setup"><?= t('how_to_set_up') ?></a>
				</div>
			</div>
			<!-- Как это выглядит: скриншот, загрузка в фоне и ссылка, которая уже в буфере обмена -->
			<div class="demo" aria-label="<?= t('demo_label') ?>">
				<div class="demo-bar" aria-hidden="true"><i></i><i></i><i></i><span class="mono">ShotHost</span></div>
				<div class="demo-row">
					<span class="demo-step mono">01</span>
					<div class="demo-body">
						<b><?= t('demo_saved') ?></b>
						<span class="demo-file mono">SCR-20261011-jvpl.png</span>
					</div>
					<span class="demo-ok" aria-hidden="true">✓</span>
				</div>
				<div class="demo-row">
					<span class="demo-step mono">02</span>
					<div class="demo-body">
						<b><?= t('demo_link') ?></b>
						<span class="demo-link mono">yakus.ru/shothost/SCR-20261011-jvpl_2af3a7d3</span>
					</div>
					<span class="demo-ok" aria-hidden="true">✓</span>
				</div>
				<div class="demo-row">
					<span class="demo-step mono">03</span>
					<div class="demo-body">
						<b><?= t('demo_upload') ?></b>
						<span class="demo-progress" aria-hidden="true"><span></span></span>
					</div>
					<span class="demo-ok demo-ok-late" aria-hidden="true">✓</span>
				</div>
			</div>
		</div>
	</section>

	<section class="product" id="features">
		<div class="wrap">
			<h2 class="section-title"><?= t('features') ?></h2>
			<ul class="features features-grid">
				<li><b><?= t('f1_title') ?></b><?= t('f1_text') ?></li>
				<li><b><?= t('f2_title') ?></b><?= t('f2_text') ?></li>
				<li><b><?= t('f3_title') ?></b><?= t('f3_text') ?></li>
				<li><b><?= t('f4_title') ?></b><?= t('f4_text') ?></li>
				<li><b><?= t('f5_title') ?></b><?= t('f5_text') ?></li>
				<li><b><?= t('f6_title') ?></b><?= t('f6_text') ?></li>
				<li><b><?= t('f7_title') ?></b><?= t('f7_text') ?></li>
				<li><b><?= t('f8_title') ?></b><?= t('f8_text') ?></li>
			</ul>
		</div>
	</section>

	<section class="product" id="download">
		<div class="wrap">
			<h2 class="section-title"><?= t('download_title') ?></h2>
			<p class="product-text"><?= t('download_text') ?></p>
			<div class="downloads">
				<a class="dl" href="https://github.com/igoryakus/shothost/releases/latest/download/ShotHost.dmg">
					<span class="dl-os"><svg class="os-icon" aria-hidden="true"><use href="#i-apple"/></svg>macOS</span>
					<span class="dl-file mono">ShotHost.dmg</span>
					<span class="dl-note"><?= t('dl_macos') ?></span>
					<span class="btn btn-primary"><?= t('download') ?></span>
				</a>
				<a class="dl" href="https://github.com/igoryakus/shothost/releases/latest/download/ShotHost.exe">
					<span class="dl-os"><svg class="os-icon" aria-hidden="true"><use href="#i-windows"/></svg>Windows</span>
					<span class="dl-file mono">ShotHost.exe</span>
					<span class="dl-note"><?= t('dl_windows') ?></span>
					<span class="btn btn-primary"><?= t('download') ?></span>
				</a>
				<a class="dl" href="https://github.com/igoryakus/shothost">
					<span class="dl-os"><svg class="os-icon" aria-hidden="true"><use href="#i-github"/></svg><?= t('dl_server') ?></span>
					<span class="dl-file mono">igoryakus/shothost</span>
					<span class="dl-note"><?= t('dl_server_text') ?></span>
					<span class="btn btn-ghost"><?= t('open_on_github') ?></span>
				</a>
			</div>
		</div>
	</section>

	<section class="product" id="setup">
		<div class="wrap">
			<h2 class="section-title"><?= t('setup_title') ?></h2>
			<p class="product-text"><?= t('setup_text') ?></p>
			<ol class="steps">
				<li class="step">
					<span class="principle-num mono">01</span>
					<h3 class="step-title"><?= t('s1_title') ?></h3>
					<p><?= t('s1_text') ?></p>
				</li>
				<li class="step">
					<span class="principle-num mono">02</span>
					<h3 class="step-title"><?= t('s2_title') ?></h3>
					<p><?= t('s2_text') ?></p>

					<h4><?= t('s2_create') ?></h4>
					<ul>
						<li><strong>macOS / Linux:</strong> <?= t('s2_mac') ?></li>
						<li><strong>Windows:</strong> <?= t('s2_win') ?></li>
					</ul>

					<h4><?= t('s2_install') ?></h4>
					<p><?= t('s2_run') ?></p>
					<pre><code>ssh-copy-id -i ~/.ssh/id_ed25519.pub user@your_server_ip</code></pre>
					<p><?= t('s2_manual') ?></p>
				</li>
				<li class="step">
					<span class="principle-num mono">03</span>
					<h3 class="step-title"><?= t('s3_title') ?></h3>
					<p><?= t('s3_text') ?></p>
					<pre><code>sudo apt update
sudo apt install nginx php-fpm git</code></pre>
				</li>
				<li class="step">
					<span class="principle-num mono">04</span>
					<h3 class="step-title"><?= t('s4_title') ?></h3>
					<p><?= t('s4_text') ?></p>
					<pre><code>cd /var/www/html
sudo git clone https://github.com/igoryakus/shothost.git
sudo chown -R $USER:www-data /var/www/html/shothost
sudo chmod 2775 /var/www/html/shothost/files</code></pre>
				</li>
				<li class="step">
					<span class="principle-num mono">05</span>
					<h3 class="step-title"><?= t('s5_title') ?></h3>
					<p><?= t('s5_text') ?></p>
					<pre><code>location /shothost/ {
    try_files $uri $uri/ /shothost/index.php?$args;

    location ~ \.php$ {
        include snippets/fastcgi-php.conf;
        fastcgi_pass unix:/var/run/php/php8.3-fpm.sock; <?= t('s5_comment') ?>
    }
}</code></pre>
					<p><?= t('s5_restart') ?></p>
				</li>
				<li class="step">
					<span class="principle-num mono">06</span>
					<h3 class="step-title"><?= t('s6_title') ?></h3>
					<p><?= t('s6_1') ?></p>
					<p><?= t('s6_2') ?></p>
					<p><?= t('s6_3') ?></p>
					<?= t('s6_fields') ?>
					<p><?= t('s6_check') ?></p>
					<p><?= t('s6_menubar') ?></p>
					<p><?= t('s6_4') ?></p>
				</li>
				<li class="step">
					<span class="principle-num mono">07</span>
					<h3 class="step-title"><?= t('s7_title') ?></h3>
					<p><?= t('s7_1') ?></p>
					<p><?= t('s7_2') ?></p>
					<p><?= t('s7_3') ?></p>
					<p><?= t('s7_note') ?></p>
					<p><?= t('s7_allow') ?></p>
				</li>
			</ol>
		</div>
	</section>
</main>
<?php elseif ($file_exists): ?>
	<!-- Картинка найдена: показываем её в Viewer.js -->
	<div id="image-gallery">
		<img id="screenshot" src="<?php echo htmlspecialchars($image_url); ?>" alt="<?= t('screenshot_alt') ?>" style="display: none;">
	</div>
<?php else: ?>
	<!-- Файл еще не докачался: показываем лоадер -->
	<div class="loader-container" id="waiting-loader" data-url="<?php echo htmlspecialchars($image_url); ?>">
		<div class="spinner" aria-hidden="true"></div>
		<h2><?= t('loading_title') ?></h2>
		<p><?= t('loading_text') ?></p>
	</div>
<?php endif; ?>

<?php include __DIR__ . '/footer.php'; ?>
