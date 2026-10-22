import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
plt.rcParams['font.family']='DejaVu Sans'
actors=["Мастер","OrderDetails-\nForm","OrderService","ShopDatabase","XmlDataStore"]
xs=[1.0,3.3,5.8,8.3,10.6]
fig,ax=plt.subplots(figsize=(11,8.2),dpi=170)
ax.set_xlim(0,11.8); ax.set_ylim(0,10.3); ax.axis('off')
for a,x in zip(actors,xs):
    ax.add_patch(Rectangle((x-0.8,9.35),1.6,0.7,fc="#EAF2FB",ec="#35618F",lw=1.2))
    ax.text(x,9.7,a,ha='center',va='center',fontsize=9,fontweight='bold')
    ax.plot([x,x],[9.35,0.3],color='#888',lw=1,ls=(0,(5,4)))
def msg(i,j,y,text,dashed=False,self_=False):
    x1,x2=xs[i],xs[j]
    if self_:
        ax.plot([x1,x1+0.9,x1+0.9,x1+0.04],[y,y,y-0.35,y-0.35],color='k',lw=1)
        ax.annotate('',xy=(x1+0.04,y-0.35),xytext=(x1+0.3,y-0.35),arrowprops=dict(arrowstyle='-|>',color='k',lw=1))
        ax.text(x1+1.0,y-0.17,text,fontsize=8,va='center')
        return
    ax.annotate('',xy=(x2,y),xytext=(x1,y),arrowprops=dict(arrowstyle='-|>',color='k',lw=1,ls='--' if dashed else '-'))
    ax.text((x1+x2)/2,y+0.12,text,fontsize=8,ha='center',va='bottom',zorder=5,bbox=dict(fc='white',ec='none',pad=1))
def act(i,y1,y2):
    ax.add_patch(Rectangle((xs[i]-0.09,y2),0.18,y1-y2,fc='white',ec='k',lw=1,zorder=3))
act(1,8.9,1.0); act(2,8.0,1.3); act(3,6.7,2.4); act(4,3.0,2.5)
msg(0,1,8.85,"нажать «Списать»")
msg(1,2,8.0,"AddPart(orderId, partId, qty)")
msg(2,2,7.4,"RequireEditable(orderId)",self_=True)
msg(2,3,6.6,"найти запчасть по partId")
msg(3,2,6.15,"Part",dashed=True)
# alt frame
ax.add_patch(Rectangle((0.3,2.2),11.2,3.8,fc='none',ec='#B5651D',lw=1.3,zorder=1))
ax.add_patch(Rectangle((0.3,5.65),0.75,0.35,fc='#FFF4DC',ec='#B5651D',lw=1.3))
ax.text(0.67,5.82,"alt",ha='center',va='center',fontsize=8,fontweight='bold')
ax.text(1.25,5.82,"[Quantity < qty]",fontsize=8,va='center')
msg(2,1,5.15,"BusinessException(«Недостаточно запчастей»)",dashed=True)
msg(1,0,4.65,"сообщение об ошибке (MessageBox)",dashed=True)
ax.plot([0.3,11.5],[4.2,4.2],color='#B5651D',lw=1,ls=(0,(6,3)))
ax.text(1.25,4.0,"[иначе]",fontsize=8,va='center')
msg(2,3,3.65,"Quantity -= qty; Parts.Add(OrderPart)")
msg(2,3,3.2,"Save()")
msg(3,4,2.85,"Save(Data)")
msg(4,3,2.45,"XML записан в data.xml",dashed=True)
msg(2,1,1.9,"возврат управления",dashed=True)
msg(1,1,1.5,"Reload(): обновить таблицы и стоимость",self_=True)
fig.savefig('sequence.png',bbox_inches='tight',facecolor='white')
