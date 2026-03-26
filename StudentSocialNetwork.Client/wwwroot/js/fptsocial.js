
document.addEventListener('click', function(e){
  const open = e.target.closest('[data-open-modal]');
  if(open){ document.getElementById(open.dataset.openModal)?.classList.add('open'); }
  const close = e.target.closest('[data-close-modal]');
  if(close){ close.closest('.modal-backdrop')?.classList.remove('open'); }
  if(e.target.classList.contains('modal-backdrop')) e.target.classList.remove('open');
  const tab = e.target.closest('.tab-btn');
  if(tab){
    const container = tab.closest('[data-tabs]');
    if(!container) return;
    container.querySelectorAll('.tab-btn').forEach(x=>x.classList.remove('active'));
    tab.classList.add('active');
    const target = tab.dataset.tab;
    container.querySelectorAll('.tab-panel').forEach(p=>p.classList.remove('active'));
    container.querySelector('.tab-panel[data-panel="'+target+'"]')?.classList.add('active');
  }
});
