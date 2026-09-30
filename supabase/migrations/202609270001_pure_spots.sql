create schema if not exists gis;
create extension if not exists postgis with schema gis;

create schema if not exists pikmin;

create table if not exists pikmin.decor_type (
    id uuid primary key default gen_random_uuid(),
    key text not null unique,
    name jsonb not null,
    display_order integer,
    constraint decor_type_name_object
        check (
            jsonb_typeof(name) = 'object'
            and name <> '{}'::jsonb
        )
);

create table if not exists pikmin.costume_type (
    id uuid primary key default gen_random_uuid(),
    decor_id uuid not null,
    key text not null,
    name jsonb not null,
    display_order integer,
    available_types text[],
    constraint costume_type_decor_fk
        foreign key (decor_id)
        references pikmin.decor_type (id),
    constraint costume_type_decor_key_unique
        unique (decor_id, key),
    constraint costume_type_name_object
        check (
            jsonb_typeof(name) = 'object'
            and name <> '{}'::jsonb
        ),
    constraint costume_type_available_types_valid
        check (
            available_types is null or
            available_types <@ array[
                'red', 'yellow', 'blue', 'white', 'purple', 'rock', 'winged', 'ice'
            ]::text[]
        )
);

create table if not exists pikmin.spot (
    id uuid primary key default gen_random_uuid(),
    name text not null,
    country text,
    city text,
    area text,
    source_id text,
    latitude double precision not null,
    longitude double precision not null,
    location gis.geography(Point, 4326)
        generated always as (
            gis.st_setsrid(
                gis.st_makepoint(longitude, latitude),
                4326
            )::gis.geography
        ) stored,
    constraint spot_latitude_range
        check (latitude between -90 and 90),
    constraint spot_longitude_range
        check (longitude between -180 and 180),
    constraint spot_coordinates_unique
        unique (latitude, longitude)
);

create table if not exists pikmin.spot_detector (
    id uuid primary key default gen_random_uuid(),
    spot_id uuid not null,
    decor_id uuid not null,
    constraint spot_detector_spot_fk
        foreign key (spot_id)
        references pikmin.spot (id),
    constraint spot_detector_decor_fk
        foreign key (decor_id)
        references pikmin.decor_type (id),
    constraint spot_detector_spot_decor_unique
        unique (spot_id, decor_id)
);

create index if not exists spot_location_gix
    on pikmin.spot
    using gist (location);

create index if not exists spot_city_area_idx
    on pikmin.spot (city, area);

create index if not exists spot_country_city_area_idx
    on pikmin.spot (country, city, area);

create index if not exists spot_detector_decor_idx
    on pikmin.spot_detector (decor_id);

-- Initial bilingual Decor and costume catalog.
-- English names follow the game category names. Chinese names follow the
-- supplied Traditional Chinese collection screenshots where available.
with catalog(key, en, zh, sort_order) as (
    values
        ('restaurant','Restaurant','餐廳',1),
        ('cafe','Café','咖啡廳',2),
        ('sweetshop','Sweetshop','甜點店',3),
        ('movie_theater','Movie Theater','電影院',4),
        ('pharmacy','Pharmacy','藥局',5),
        ('zoo','Zoo','動物園',6),
        ('forest','Forest','森林',7),
        ('waterside','Waterside','水邊',8),
        ('post_office','Post Office','郵局',9),
        ('art_gallery','Art Gallery','美術館',10),
        ('airport','Airport','機場',11),
        ('station','Station','車站',12),
        ('beach','Beach','海灘',13),
        ('burger_place','Burger Place','漢堡店',14),
        ('corner_store','Corner Store','便利店',15),
        ('supermarket','Supermarket','超市',16),
        ('bakery','Bakery','麵包店',17),
        ('hair_salon','Hair Salon','美容院',18),
        ('clothes_store','Clothes Store','服裝店',19),
        ('park','Park','公園',20),
        ('library_and_bookstore','Library & Bookstore','圖書館',21),
        ('sushi_restaurant','Sushi Restaurant','壽司餐廳',22),
        ('mountain','Mountain','山丘',23),
        ('stadium','Stadium','體育館',24),
        ('theme_park','Theme Park','主題樂園',25),
        ('bus_stop','Bus Stop','公車站',26),
        ('italian_restaurant','Italian Restaurant','義式餐廳',27),
        ('ramen_restaurant','Ramen Restaurant','拉麵店',28),
        ('bridge','Bridge','橋樑',29),
        ('hotel','Hotel','飯店',30),
        ('makeup_store','Makeup Store','化妝品商店',31),
        ('shrine_and_temple','Shrine & Temple','神社和寺廟',32),
        ('appliances_store','Appliances Store','電器行',33),
        ('curry_restaurant','Curry Restaurant','咖哩餐廳',34),
        ('diy_store','DIY Store','五金行',35),
        ('university_and_college','University & College','大學&學院',36),
        ('mexican_restaurant','Mexican Restaurant','墨西哥餐廳',37),
        ('laundromats_and_dry_cleaners','Laundromats & Dry Cleaners','自助洗衣店&乾洗店',38),
        ('korean_restaurant','Korean Restaurant','韓國餐廳',39),
        ('stationery_store','Stationery Store','文具店',40),
        ('roadside','Roadside','路邊',41),
        ('rainy_day','Rainy Day','下雨',42),
        ('snowy_day','Snowy Day','下雪',43)
)
insert into pikmin.decor_type(key, name, display_order)
select key, jsonb_build_object('en', en, 'zh-TW', zh), sort_order from catalog
on conflict (key) do update set
    name = pikmin.decor_type.name || excluded.name,
    display_order = excluded.display_order;

-- Only verified grids have an availability array. An unverified grid must not
-- be used to infer missing Pikmin from a screenshot.
with catalog(decor_key, costume_key, en, zh, sort_order, pikmin_types) as (
    values
        ('cafe','coffee_cup','Coffee Cup','咖啡杯',1,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('cafe','rare_coffee_cup','Rare Coffee Cup','稀有咖啡杯',2,array['red','yellow','blue','white','purple','rock','winged']),
        ('bakery','baguette','Baguette','法國麵包',1,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('bakery','pastry','Pastry','糕點',2,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('hair_salon','scissors','Scissors','剪刀',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('clothes_store','hair_tie','Hair Tie','髮圈',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('park','clover','Clover','三葉草',1,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('park','four_leaf_clover','Four-Leaf Clover','四葉草',2,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('library_and_bookstore','tiny_book','Tiny Book','迷你書',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('station','paper_train','Paper Train','紙火車',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('station','ticket','Ticket','車票',2,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('burger_place','burger','Burger','漢堡',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('corner_store','bottle_cap','Bottle Cap','瓶蓋',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('corner_store','snack','Snack','點心',2,array['red','yellow','blue','white','purple','rock','winged']),
        ('supermarket','mushroom','Mushroom','蘑菇',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('supermarket','banana','Banana','香蕉',2,array['red','yellow','blue','white','purple','rock','winged']),
        ('roadside','green_sticker','Green Sticker','綠色貼紙',1,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('roadside','blue_sticker','Blue Sticker','藍色貼紙',2,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('roadside','orange_sticker','Orange Sticker','橘色貼紙',3,array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('roadside','coin','Coin','硬幣',4,array['red','yellow','blue','white','purple','rock','winged','ice'])
)
insert into pikmin.costume_type(decor_id, key, name, display_order, available_types)
select d.id, c.costume_key, jsonb_build_object('en', c.en, 'zh-TW', c.zh),
       c.sort_order, c.pikmin_types
from catalog c join pikmin.decor_type d on d.key = c.decor_key
on conflict (decor_id, key) do update set
    name = pikmin.costume_type.name || excluded.name,
    display_order = excluded.display_order,
    available_types = excluded.available_types;

-- The remaining single-grid motifs receive availability below when their
-- Pikmin coverage is known.
with catalog(decor_key, costume_key, en, zh, sort_order) as (
    values
        ('restaurant','chef_hat','Chef Hat','廚師帽',1),
        ('restaurant','rare_chef_hat','Rare Chef Hat','稀有廚師帽',2),
        ('sweetshop','macaron','Macaron','馬卡龍',1),
        ('sweetshop','donut','Donut','甜甜圈',2),
        ('movie_theater','popcorn_snack','Popcorn Snack','爆米花',1),
        ('pharmacy','toothbrush','Toothbrush','牙刷',1),
        ('pharmacy','rare_toothbrush','Rare Toothbrush','稀有牙刷',2),
        ('zoo','dandelion','Dandelion','蒲公英',1),
        ('forest','stag_beetle','Stag Beetle','鍬形蟲',1),
        ('forest','acorn','Acorn','橡實',2),
        ('forest','rare_stag_beetle','Rare Stag Beetle','稀有鍬形蟲',3),
        ('forest','rare_acorn','Rare Acorn','稀有橡實',4),
        ('waterside','fishing_lure','Fishing Lure','釣魚擬餌',1),
        ('waterside','rare_fishing_lure','Rare Fishing Lure','稀有釣魚擬餌',2),
        ('post_office','stamp','Stamp','郵票',1),
        ('art_gallery','picture_frame','Picture Frame','畫框',1),
        ('airport','toy_airplane','Toy Airplane','玩具飛機',1),
        ('airport','luggage_tag','Luggage Tag','行李吊牌',2),
        ('station','rare_ticket','Rare Ticket','稀有車票',3),
        ('station','gold_ticket','Gold Ticket','金色車票',4),
        ('beach','shell','Shell','貝殼',1),
        ('supermarket','rare_banana','Rare Banana','稀有香蕉',3),
        ('bakery','rare_baguette','Rare Baguette','稀有法國麵包',3),
        ('hair_salon','rare_scissors','Rare Scissors','稀有剪刀',2),
        ('park','rare_clover','Rare Clover','稀有三葉草',3),
        ('park','rare_four_leaf_clover','Rare Four-Leaf Clover','稀有四葉草',4),
        ('sushi_restaurant','sushi','Sushi','壽司',1),
        ('mountain','mountain_pin_badge','Mountain Pin Badge','山區徽章',1),
        ('stadium','ball_keychain','Ball Keychain','球類鑰匙圈',1),
        ('stadium','rare_ball_keychain','Rare Ball Keychain','稀有球類鑰匙圈',2),
        ('bus_stop','bus_papercraft','Bus Papercraft','紙模型公車',1),
        ('italian_restaurant','pizza','Pizza','披薩',1),
        ('italian_restaurant','pasta','Pasta','義大利麵',2),
        ('ramen_restaurant','ramen_keychain','Ramen Keychain','拉麵鑰匙圈',1),
        ('bridge','bridge_pin_badge','Bridge Pin Badge','橋梁徽章',1),
        ('hotel','hotel_amenities','Hotel Amenities','飯店備品',1),
        ('makeup_store','makeup','Makeup','化妝品',1),
        ('curry_restaurant','curry_bowl','Curry Bowl','咖哩碗',1),
        ('diy_store','tool','Tool','工具',1),
        ('university_and_college','college_crest_patch','College Crest Patch','校徽布章',1),
        ('mexican_restaurant','taco','Taco','塔可餅',1),
        ('laundromats_and_dry_cleaners','laundry_item','Laundry Item','洗衣用品',1),
        ('korean_restaurant','kimchi','Kimchi','泡菜',1),
        ('stationery_store','stationery','Stationery','文具',1),
        ('roadside','rare_coin','Rare Coin','稀有硬幣',5)
)
insert into pikmin.costume_type(decor_id, key, name, display_order)
select d.id, c.costume_key, jsonb_build_object('en', c.en, 'zh-TW', c.zh), c.sort_order
from catalog c join pikmin.decor_type d on d.key = c.decor_key
on conflict (decor_id, key) do update set
    name = pikmin.costume_type.name || excluded.name,
    display_order = excluded.display_order;

-- These designs occupy separate collection grids and need distinct keys.
with catalog(decor_key, costume_key, en, zh, sort_order, pikmin_types) as (
    values
        ('shrine_and_temple','great_blessing','Great Blessing','大吉',1,array['red','yellow','blue','white','purple','rock','winged']),
        ('shrine_and_temple','blessing','Blessing','吉',2,array['red','yellow','blue','white','purple','rock','winged']),
        ('shrine_and_temple','middle_blessing','Middle Blessing','中吉',3,array['red','yellow','blue','white','purple','rock','winged']),
        ('shrine_and_temple','small_blessing','Small Blessing','小吉',4,array['red','yellow','blue','white','purple','rock','winged']),
        ('shrine_and_temple','future_blessing','Future Blessing','末吉',5,array['red','yellow','blue','white','purple','rock','winged']),
        ('theme_park','ferris_wheel_ticket','Ferris Wheel Ticket','摩天輪門票',1,array['red','yellow','blue']),
        ('theme_park','pirate_ship_ticket','Pirate Ship Ticket','海盜船門票',2,array['red','yellow','blue'])
)
insert into pikmin.costume_type(decor_id, key, name, display_order, available_types)
select d.id, c.costume_key, jsonb_build_object('en', c.en, 'zh-TW', c.zh),
       c.sort_order, c.pikmin_types
from catalog c join pikmin.decor_type d on d.key = c.decor_key
on conflict (decor_id, key) do update set
    name = pikmin.costume_type.name || excluded.name,
    display_order = excluded.display_order,
    available_types = excluded.available_types;

update pikmin.costume_type c
set available_types = array['red','yellow','blue','white','purple','rock','winged']
from pikmin.decor_type d
where c.decor_id = d.id and d.key = 'bakery' and c.key = 'rare_baguette';

update pikmin.costume_type c
set available_types = array['red','yellow','blue','white','purple','rock','winged','ice']
from pikmin.decor_type d
where c.decor_id = d.id and d.key = 'station' and c.key = 'rare_ticket';

update pikmin.costume_type c
set available_types = array['red']
from pikmin.decor_type d
where c.decor_id = d.id and d.key = 'station' and c.key = 'gold_ticket';

with catalog(costume_key, en, zh, sort_order) as (
    values
        ('battery_1','Battery 1','電池 1',1),
        ('battery_2','Battery 2','電池 2',2),
        ('battery_3','Battery 3','電池 3',3),
        ('battery_4','Battery 4','電池 4',4),
        ('battery_5','Battery 5','電池 5',5),
        ('battery_6','Battery 6','電池 6',6),
        ('fairy_lights_1','Fairy Lights 1','燈飾 1',7),
        ('fairy_lights_2','Fairy Lights 2','燈飾 2',8)
)
insert into pikmin.costume_type(decor_id, key, name, display_order, available_types)
select d.id, c.costume_key, jsonb_build_object('en', c.en, 'zh-TW', c.zh),
       c.sort_order, array['yellow']
from catalog c join pikmin.decor_type d on d.key = 'appliances_store'
on conflict (decor_id, key) do update set
    name = pikmin.costume_type.name || excluded.name,
    display_order = excluded.display_order,
    available_types = excluded.available_types;

-- Weather collections have a small fixed set of designs. Each design occupies
-- one slot, so it needs its own key even when all slots share a Pikmin color.
with catalog(decor_key, costume_key, en, zh, sort_order, pikmin_types) as (
    values
        ('rainy_day','leaf_hat_1','Leaf Hat 1','葉片帽 1',1,array['blue']),
        ('rainy_day','leaf_hat_2','Leaf Hat 2','葉片帽 2',2,array['blue']),
        ('rainy_day','leaf_hat_3','Leaf Hat 3','葉片帽 3',3,array['blue']),
        ('snowy_day','snow','Snow','雪',1,array['blue','white','ice'])
)
insert into pikmin.costume_type(decor_id, key, name, display_order, available_types)
select d.id, c.costume_key, jsonb_build_object('en', c.en, 'zh-TW', c.zh),
       c.sort_order, c.pikmin_types
from catalog c join pikmin.decor_type d on d.key = c.decor_key
on conflict (decor_id, key) do update set
    name = pikmin.costume_type.name || excluded.name,
    display_order = excluded.display_order,
    available_types = excluded.available_types;

with coverage(decor_key, pikmin_types) as (
    values
        ('restaurant',array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('sweetshop',array['red','yellow','blue','white','purple','rock','winged']),
        ('movie_theater',array['red','yellow','blue','white','purple','rock','winged']),
        ('pharmacy',array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('zoo',array['red','yellow','blue','white','purple','rock','winged']),
        ('forest',array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('waterside',array['red','yellow','blue','white','purple','rock','winged']),
        ('post_office',array['red','yellow','blue','white','purple','rock','winged']),
        ('art_gallery',array['red','yellow','blue','white','purple','rock','winged']),
        ('airport',array['red','yellow','blue','white','purple','rock','winged']),
        ('beach',array['red','yellow','blue','white','purple','rock','winged']),
        ('sushi_restaurant',array['red','yellow','blue','white','purple','rock','winged']),
        ('mountain',array['red','yellow','blue','white','purple','rock','winged']),
        ('stadium',array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('bus_stop',array['red','yellow','blue','white','purple','rock','winged']),
        ('italian_restaurant',array['red','yellow','blue','white','purple','rock','winged']),
        ('ramen_restaurant',array['red','yellow','blue','white','purple','rock','winged']),
        ('bridge',array['red','yellow','blue','white','purple','rock','winged']),
        ('hotel',array['red','yellow','blue','white','purple','rock','winged']),
        ('makeup_store',array['red','yellow','blue','white','purple','rock','winged']),
        ('curry_restaurant',array['red','yellow','blue','white','purple','rock','winged']),
        ('diy_store',array['red','yellow','blue','white','purple','rock','winged']),
        ('university_and_college',array['red','yellow','blue']),
        ('mexican_restaurant',array['red','yellow','blue','white','purple','rock','winged']),
        ('laundromats_and_dry_cleaners',array['red','yellow','blue','white','purple','rock','winged']),
        ('korean_restaurant',array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('stationery_store',array['red','yellow','blue','white','purple','rock','winged','ice']),
        ('roadside',array['red','yellow','blue','white','purple','rock','winged','ice'])
)
update pikmin.costume_type c
set available_types = coverage.pikmin_types
from pikmin.decor_type d join coverage on coverage.decor_key = d.key
where c.decor_id = d.id and c.available_types is null;
