<!DOCTYPE html>
<html lang="<?= $lang ?? 'en' ?>">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title><?php echo htmlspecialchars($page_title); ?></title>
	<?php if (!empty($page_description)): ?>
	<meta name="description" content="<?php echo htmlspecialchars($page_description); ?>">
	<?php endif; ?>
	<meta name="theme-color" content="#000000">
	<?php if (!empty($is_home)): ?>
	<link rel="alternate" hreflang="en" href="https://yakus.ru/shothost/">
	<link rel="alternate" hreflang="ru" href="https://yakus.ru/shothost/ru/">
	<link rel="alternate" hreflang="x-default" href="https://yakus.ru/shothost/">
	<?php endif; ?>
	<!-- Иконка общая для всего yakus.ru -->
	<link rel="icon" href="/favicon.svg" type="image/svg+xml">
	<link rel="apple-touch-icon" href="/apple-touch-icon.png">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@500&family=Michroma&display=swap">
	<?php if (!empty($use_viewer)): ?>
	<!-- Стили плагина Viewer.js -->
	<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/viewerjs/1.11.7/viewer.min.css">
	<?php endif; ?>
	<link rel="stylesheet" href="/shothost/assets/style.css?v=6">
</head>
<body class="<?php echo htmlspecialchars($body_class ?? ''); ?>">
