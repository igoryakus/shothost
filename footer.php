<?php if (empty($use_viewer)): ?>
<footer class="footer">
	<div class="wrap footer-inner">
		<span>© <?php echo date('Y'); ?> <?= t('footer') ?></span>
		<a class="mono" href="<?= $site_url ?>">yakus.ru</a>
	</div>
</footer>
<?php endif; ?>

<?php if (!empty($use_viewer)): ?>
<!-- Viewer.js нужен только на странице скриншота -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/viewerjs/1.11.7/viewer.min.js"></script>
<?php endif; ?>
<script src="/shothost/assets/script.js?v=2"></script>
</body>
</html>
