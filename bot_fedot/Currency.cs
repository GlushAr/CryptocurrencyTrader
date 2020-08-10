using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bot_fedot
{
    class Currency
    {
        public float high;             //- максимальная цена сделки за 24 часа
        public float low;              //- минимальная цена сделки за 24 часа
        public float avg;              //- средняя цена сделки за 24 часа
        public float vol;              //- объем всех сделок за 24 часа
        public float vol_curr;         //- сумма всех сделок за 24 часа
        public float last_trade;       //- цена последней сделки
        public float buy_price;        //- текущая максимальная цена покупки
        public float sell_price;       //- текущая минимальная цена продажи
        public int updated;            //- дата и время обновления данных
    }
}
